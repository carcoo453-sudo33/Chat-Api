using System.Linq.Expressions;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
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
                return _db.Users.Values.Where(u => u.IsOnline).ToList();
            return await _col!.Find(u => u.IsOnline).ToListAsync();
        }

        public async Task SetStatusAsync(string id, bool isOnline)
        {
            var u = await GetByIdAsync(id);
            if (u is null) return;
            u.IsOnline = isOnline;
            u.LastSeen = DateTime.UtcNow;
            await UpdateAsync(u);
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

        public async Task<List<ChatUser>> SearchAsync(string query, int skip, int take)
        {
            query = query.ToLower();
            if (_db.IsInMemory)
                return _db.Users.Values
                    .Where(u => u.Username.Contains(query, StringComparison.OrdinalIgnoreCase)
                             || u.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                             || u.Email.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Skip(skip).Take(take).ToList();

            var filter = Builders<ChatUser>.Filter.Or(
                Builders<ChatUser>.Filter.Regex(u => u.Username,    new MongoDB.Bson.BsonRegularExpression(query, "i")),
                Builders<ChatUser>.Filter.Regex(u => u.DisplayName, new MongoDB.Bson.BsonRegularExpression(query, "i"))
            );
            return await _col!.Find(filter).Skip(skip).Limit(take).ToListAsync();
        }
    }
}
