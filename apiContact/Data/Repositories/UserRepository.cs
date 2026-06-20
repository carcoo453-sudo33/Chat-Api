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
            if (_db.IsInMemory) return _db.Users.GetValueOrDefault(id);
            return await _col!.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<ChatUser>> GetAllAsync()
        {
            if (_db.IsInMemory) return _db.Users.Values.OrderBy(u => u.DisplayName).ToList();
            return await _col!.Find(_ => true).SortBy(u => u.DisplayName).ToListAsync();
        }

        public async Task<List<ChatUser>> FindAsync(Expression<Func<ChatUser, bool>> predicate)
        {
            if (_db.IsInMemory) return _db.Users.Values.Where(predicate.Compile()).ToList();
            return await _col!.Find(predicate).ToListAsync();
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

        public async Task<bool> DeleteAsync(string id)
        {
            if (_db.IsInMemory) return _db.Users.Remove(id);
            var r = await _col!.DeleteOneAsync(u => u.Id == id);
            return r.DeletedCount > 0;
        }

        public async Task<int> CountAsync(Expression<Func<ChatUser, bool>>? predicate = null)
        {
            if (_db.IsInMemory)
            {
                var src = _db.Users.Values.AsEnumerable();
                return predicate is null ? src.Count() : src.Count(predicate.Compile());
            }
            var filter = predicate ?? (_ => true);
            return (int)await _col!.CountDocumentsAsync(filter);
        }

        public async Task<bool> ExistsAsync(Expression<Func<ChatUser, bool>> predicate)
        {
            if (_db.IsInMemory) return _db.Users.Values.Any(predicate.Compile());
            return await _col!.Find(predicate).AnyAsync();
        }

        // ── IUserRepository ──────────────────────────────────────
        public async Task<ChatUser?> GetByUsernameAsync(string username)
        {
            if (_db.IsInMemory)
                return _db.Users.Values.FirstOrDefault(
                    u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return await _col!.Find(u => u.Username == username.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<ChatUser?> GetByEmailAsync(string email)
        {
            if (_db.IsInMemory)
                return _db.Users.Values.FirstOrDefault(
                    u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return await _col!.Find(u => u.Email == email.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<List<ChatUser>> GetOnlineUsersAsync()
        {
            if (_db.IsInMemory)
                return _db.Users.Values.Where(u => u.Status == UserStatus.Online).ToList();
            return await _col!.Find(u => u.Status == UserStatus.Online).ToListAsync();
        }

        public async Task SetStatusAsync(string id, UserStatus status)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return;
            u.Status  = status;
            u.LastSeen = DateTime.UtcNow;
            if (_db.IsInMemory) { _db.Users[id] = u; return; }
            await _col!.UpdateOneAsync(x => x.Id == id,
                Builders<ChatUser>.Update
                    .Set(x => x.Status,   status)
                    .Set(x => x.LastSeen, DateTime.UtcNow));
        }

        public async Task SaveRefreshTokenAsync(string id, string? token, DateTime? expiry)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return;
            u.RefreshToken       = token;
            u.RefreshTokenExpiry = expiry;
            if (_db.IsInMemory) { _db.Users[id] = u; return; }
            await _col!.UpdateOneAsync(x => x.Id == id,
                Builders<ChatUser>.Update
                    .Set(x => x.RefreshToken,       token)
                    .Set(x => x.RefreshTokenExpiry, expiry));
        }

        public async Task<bool> ChangePasswordAsync(string id, string newHash)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return false;
            u.PasswordHash = newHash;
            if (_db.IsInMemory) { _db.Users[id] = u; return true; }
            await _col!.UpdateOneAsync(x => x.Id == id,
                Builders<ChatUser>.Update.Set(x => x.PasswordHash, newHash));
            return true;
        }

        public async Task<List<ChatUser>> SearchAsync(UserSearchQuery q)
        {
            q.Clamp();
            if (_db.IsInMemory)
                return ApplyFilter(_db.Users.Values, q).Skip(q.Skip).Take(q.PageSize).ToList();

            var filter = BuildMongoFilter(q);
            return await _col!.Find(filter).Skip(q.Skip).Limit(q.PageSize).ToListAsync();
        }

        public async Task<int> CountSearchAsync(UserSearchQuery q)
        {
            if (_db.IsInMemory)
                return ApplyFilter(_db.Users.Values, q).Count();
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
            var filters = new List<FilterDefinition<ChatUser>>();

            if (!string.IsNullOrWhiteSpace(q.Q))
                filters.Add(b.Or(
                    b.Regex(u => u.Username,    new MongoDB.Bson.BsonRegularExpression(q.Q, "i")),
                    b.Regex(u => u.DisplayName, new MongoDB.Bson.BsonRegularExpression(q.Q, "i"))));

            if (q.Role.HasValue)
                filters.Add(b.Eq(u => u.Role, q.Role.Value.ToString()));

            if (q.OnlineOnly == true)
                filters.Add(b.Eq(u => u.Status, UserStatus.Online));

            return filters.Count > 0 ? b.And(filters) : b.Empty;
        }
    }
}
