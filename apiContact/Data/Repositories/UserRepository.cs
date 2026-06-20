using System.Linq.Expressions;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Models.Enums;
using MongoDB.Driver;

namespace apiContact.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ChatDbContext _db;
        private readonly IMongoCollection<ChatUser>? _col;

        public UserRepository(ChatDbContext db)
        {
            _db  = db;
            _col = db.GetCollection<ChatUser>("users");
        }

        // ── IRepository<ChatUser> ────────────────────────────────

        public async Task<ChatUser?> GetByIdAsync(string id)
        {
            if (_db.IsInMemory)
            {
                var u = _db.Users.GetValueOrDefault(id);
                return u is { IsDeleted: false } ? u : null;
            }
            return await _col!.Find(u => u.Id == id && !u.IsDeleted).FirstOrDefaultAsync();
        }

        public async Task<List<ChatUser>> GetAllAsync()
        {
            if (_db.IsInMemory)
                return _db.Users.Values
                    .Where(u => !u.IsDeleted)
                    .OrderBy(u => u.DisplayName)
                    .ToList();

            return await _col!
                .Find(u => !u.IsDeleted)
                .SortBy(u => u.DisplayName)
                .ToListAsync();
        }

        public async Task<List<ChatUser>> FindAsync(Expression<Func<ChatUser, bool>> predicate)
        {
            if (_db.IsInMemory)
                return _db.Users.Values
                    .Where(u => !u.IsDeleted)
                    .Where(predicate.Compile())
                    .ToList();

            var notDeleted = Builders<ChatUser>.Filter.Eq(u => u.IsDeleted, false);
            return await _col!.Find(notDeleted & predicate).ToListAsync();
        }

        public async Task<ChatUser> AddAsync(ChatUser entity)
        {
            if (_db.IsInMemory) { _db.Users[entity.Id] = entity; return entity; }
            await _col!.InsertOneAsync(entity);
            return entity;
        }

        public async Task<ChatUser?> UpdateAsync(ChatUser entity)
        {
            if (_db.IsInMemory) { _db.Users[entity.Id] = entity; return entity; }
            await _col!.ReplaceOneAsync(u => u.Id == entity.Id, entity);
            return entity;
        }

        /// <summary>
        /// Soft-delete: marks the user as deleted rather than removing the record.
        /// This preserves audit trails and message authorship references.
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            var user = await GetByIdAsync(id);
            if (user is null) return false;

            user.SoftDelete();

            if (_db.IsInMemory) { _db.Users[id] = user; return true; }

            await _col!.UpdateOneAsync(u => u.Id == id,
                Builders<ChatUser>.Update
                    .Set(u => u.IsDeleted,  true)
                    .Set(u => u.DeletedAt,  user.DeletedAt)
                    .Set(u => u.UpdatedAt,  user.UpdatedAt));
            return true;
        }

        public async Task<int> CountAsync(Expression<Func<ChatUser, bool>>? predicate = null)
        {
            if (_db.IsInMemory)
            {
                var src = _db.Users.Values.Where(u => !u.IsDeleted);
                return predicate is null ? src.Count() : src.Count(predicate.Compile());
            }
            var notDeleted = Builders<ChatUser>.Filter.Eq(u => u.IsDeleted, false);
            var combined   = predicate is null ? notDeleted : notDeleted & predicate;
            return (int)await _col!.CountDocumentsAsync(combined);
        }

        public async Task<bool> ExistsAsync(Expression<Func<ChatUser, bool>> predicate)
        {
            if (_db.IsInMemory)
                return _db.Users.Values.Where(u => !u.IsDeleted).Any(predicate.Compile());

            var notDeleted = Builders<ChatUser>.Filter.Eq(u => u.IsDeleted, false);
            return await _col!.Find(notDeleted & predicate).AnyAsync();
        }

        // ── IUserRepository ──────────────────────────────────────

        public async Task<ChatUser?> GetByUsernameAsync(string username)
        {
            if (_db.IsInMemory)
                return _db.Users.Values.FirstOrDefault(u =>
                    !u.IsDeleted &&
                    u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            return await _col!
                .Find(u => !u.IsDeleted && u.Username == username.ToLower())
                .FirstOrDefaultAsync();
        }

        public async Task<ChatUser?> GetByEmailAsync(string email)
        {
            if (_db.IsInMemory)
                return _db.Users.Values.FirstOrDefault(u =>
                    !u.IsDeleted &&
                    u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            return await _col!
                .Find(u => !u.IsDeleted && u.Email == email.ToLower())
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Lookup by refresh token — O(1) indexed query.
        /// Fixes the DoS vulnerability where Refresh loaded the entire user table.
        /// </summary>
        public async Task<ChatUser?> GetByRefreshTokenAsync(string refreshToken)
        {
            if (_db.IsInMemory)
                return _db.Users.Values.FirstOrDefault(u =>
                    !u.IsDeleted &&
                    u.RefreshToken == refreshToken &&
                    u.RefreshTokenExpiry > DateTime.UtcNow);

            return await _col!
                .Find(u =>
                    !u.IsDeleted &&
                    u.RefreshToken == refreshToken &&
                    u.RefreshTokenExpiry > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ChatUser>> GetOnlineUsersAsync()
        {
            if (_db.IsInMemory)
                return _db.Users.Values
                    .Where(u => !u.IsDeleted && u.Status == UserStatus.Online)
                    .ToList();

            return await _col!
                .Find(u => !u.IsDeleted && u.Status == UserStatus.Online)
                .ToListAsync();
        }

        public async Task SetStatusAsync(string id, UserStatus status)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return;
            u.Status    = status;
            u.LastSeen  = DateTime.UtcNow;
            u.UpdatedAt = DateTime.UtcNow;
            if (_db.IsInMemory) { _db.Users[id] = u; return; }
            await _col!.UpdateOneAsync(x => x.Id == id,
                Builders<ChatUser>.Update
                    .Set(x => x.Status,    status)
                    .Set(x => x.LastSeen,  u.LastSeen)
                    .Set(x => x.UpdatedAt, u.UpdatedAt));
        }

        public async Task SaveRefreshTokenAsync(string id, string? token, DateTime? expiry)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return;
            u.RefreshToken       = token;
            u.RefreshTokenExpiry = expiry;
            u.UpdatedAt          = DateTime.UtcNow;
            if (_db.IsInMemory) { _db.Users[id] = u; return; }
            await _col!.UpdateOneAsync(x => x.Id == id,
                Builders<ChatUser>.Update
                    .Set(x => x.RefreshToken,       token)
                    .Set(x => x.RefreshTokenExpiry, expiry)
                    .Set(x => x.UpdatedAt,          u.UpdatedAt));
        }

        public async Task<bool> ChangePasswordAsync(string id, string newHash)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return false;
            u.PasswordHash = newHash;
            u.UpdatedAt    = DateTime.UtcNow;
            if (_db.IsInMemory) { _db.Users[id] = u; return true; }
            await _col!.UpdateOneAsync(x => x.Id == id,
                Builders<ChatUser>.Update
                    .Set(x => x.PasswordHash, newHash)
                    .Set(x => x.UpdatedAt,    u.UpdatedAt));
            return true;
        }

        public async Task<List<ChatUser>> SearchAsync(UserSearchQuery q)
        {
            q.Clamp();
            if (_db.IsInMemory)
                return ApplyFilter(_db.Users.Values.Where(u => !u.IsDeleted), q)
                    .Skip(q.Skip).Take(q.PageSize).ToList();

            var filter = BuildMongoFilter(q);
            return await _col!.Find(filter).Skip(q.Skip).Limit(q.PageSize).ToListAsync();
        }

        public async Task<int> CountSearchAsync(UserSearchQuery q)
        {
            if (_db.IsInMemory)
                return ApplyFilter(_db.Users.Values.Where(u => !u.IsDeleted), q).Count();

            return (int)await _col!.CountDocumentsAsync(BuildMongoFilter(q));
        }

        // ── Helpers ──────────────────────────────────────────────

        private static IEnumerable<ChatUser> ApplyFilter(
            IEnumerable<ChatUser> src, UserSearchQuery q)
        {
            if (!string.IsNullOrWhiteSpace(q.Q))
                src = src.Where(u =>
                    u.Username.Contains(q.Q,    StringComparison.OrdinalIgnoreCase) ||
                    u.DisplayName.Contains(q.Q, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(q.Q,       StringComparison.OrdinalIgnoreCase));

            if (q.Role.HasValue)
                src = src.Where(u => u.RoleEnum == q.Role.Value);

            if (q.OnlineOnly == true)
                src = src.Where(u => u.Status == UserStatus.Online);

            return src.OrderBy(u => u.DisplayName);
        }

        private static FilterDefinition<ChatUser> BuildMongoFilter(UserSearchQuery q)
        {
            var b       = Builders<ChatUser>.Filter;
            var filters = new List<FilterDefinition<ChatUser>>
            {
                b.Eq(u => u.IsDeleted, false)
            };

            if (!string.IsNullOrWhiteSpace(q.Q))
                filters.Add(b.Or(
                    b.Regex(u => u.Username,    new MongoDB.Bson.BsonRegularExpression(q.Q, "i")),
                    b.Regex(u => u.DisplayName, new MongoDB.Bson.BsonRegularExpression(q.Q, "i"))));

            if (q.Role.HasValue)
                filters.Add(b.Eq(u => u.Role, q.Role.Value.ToString()));

            if (q.OnlineOnly == true)
                filters.Add(b.Eq(u => u.Status, UserStatus.Online));

            return b.And(filters);
        }
    }
}
