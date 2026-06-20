using System.Linq.Expressions;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Models.Enums;
using MongoDB.Driver;

namespace apiContact.Data.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ChatDbContext _db;
        private readonly IMongoCollection<Message>? _col;

        public MessageRepository(ChatDbContext db)
        {
            _db  = db;
            _col = db.GetCollection<Message>("messages");
        }

        // ── IRepository<Message> ─────────────────────────────────

        public async Task<Message?> GetByIdAsync(string id)
        {
            if (_db.IsInMemory) return _db.Messages.GetValueOrDefault(id);
            return await _col!.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<Message>> GetAllAsync()
        {
            if (_db.IsInMemory)
                return _db.Messages.Values.OrderBy(m => m.Timestamp).ToList();
            return await _col!.Find(_ => true).SortBy(m => m.Timestamp).ToListAsync();
        }

        public async Task<List<Message>> FindAsync(Expression<Func<Message, bool>> predicate)
        {
            if (_db.IsInMemory) return _db.Messages.Values.Where(predicate.Compile()).ToList();
            return await _col!.Find(predicate).ToListAsync();
        }

        public async Task<Message> AddAsync(Message entity)
        {
            if (_db.IsInMemory) { _db.Messages[entity.Id] = entity; return entity; }
            await _col!.InsertOneAsync(entity);
            return entity;
        }

        public async Task<Message?> UpdateAsync(Message entity)
        {
            if (_db.IsInMemory) { _db.Messages[entity.Id] = entity; return entity; }
            await _col!.ReplaceOneAsync(m => m.Id == entity.Id, entity);
            return entity;
        }

        /// <summary>
        /// Soft-delete: keeps the record visible (as "[Message deleted]") so
        /// conversation flow is not broken. Uses BaseEntity.SoftDelete() for
        /// consistent IsDeleted / DeletedAt / UpdatedAt stamping.
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            var msg = await GetByIdAsync(id);
            if (msg is null) return false;

            msg.SoftDelete();
            msg.Content = "[Message deleted]";

            if (_db.IsInMemory) { _db.Messages[id] = msg; return true; }

            await _col!.UpdateOneAsync(m => m.Id == id,
                Builders<Message>.Update
                    .Set(m => m.IsDeleted, true)
                    .Set(m => m.DeletedAt, msg.DeletedAt)
                    .Set(m => m.UpdatedAt, msg.UpdatedAt)
                    .Set(m => m.Content,   "[Message deleted]"));
            return true;
        }

        public async Task<int> CountAsync(Expression<Func<Message, bool>>? predicate = null)
        {
            if (_db.IsInMemory)
            {
                var src = _db.Messages.Values.AsEnumerable();
                return predicate is null ? src.Count() : src.Count(predicate.Compile());
            }
            var filter = predicate ?? (_ => true);
            return (int)await _col!.CountDocumentsAsync(filter);
        }

        public async Task<bool> ExistsAsync(Expression<Func<Message, bool>> predicate)
        {
            if (_db.IsInMemory) return _db.Messages.Values.Any(predicate.Compile());
            return await _col!.Find(predicate).AnyAsync();
        }

        // ── IMessageRepository ───────────────────────────────────

        /// <summary>
        /// Returns up to <paramref name="limit"/> messages for a room, ordered chronologically
        /// (oldest first) for display. Internally queries newest-first and re-sorts in memory
        /// so that pagination always walks backwards in time consistently across both backends.
        /// </summary>
        public async Task<List<Message>> GetByRoomAsync(string roomId, int limit, int skip)
        {
            if (_db.IsInMemory)
            {
                // Step 1: newest-first to pick the right page window.
                // Step 2: reverse to chronological order for display.
                return _db.Messages.Values
                    .Where(m => m.RoomId == roomId && !m.IsDeleted)
                    .OrderByDescending(m => m.Timestamp)
                    .Skip(skip).Take(limit)
                    .OrderBy(m => m.Timestamp)
                    .ToList();
            }

            // MongoDB: query newest-first, paginate, then sort ascending in memory.
            // Chaining two Sort calls in the driver replaces the first, so we re-sort
            // the fetched page in application memory instead.
            var page = await _col!
                .Find(m => m.RoomId == roomId && !m.IsDeleted)
                .SortByDescending(m => m.Timestamp)
                .Skip(skip).Limit(limit)
                .ToListAsync();

            return page.OrderBy(m => m.Timestamp).ToList();
        }

        public async Task<List<Message>> SearchAsync(string roomId, MessageSearchQuery q)
        {
            q.Clamp();
            if (_db.IsInMemory)
                return ApplyFilter(_db.Messages.Values, roomId, q)
                    .Skip(q.Skip).Take(q.PageSize).ToList();

            return await _col!.Find(BuildMongoFilter(roomId, q))
                .SortByDescending(m => m.Timestamp)
                .Skip(q.Skip).Limit(q.PageSize)
                .ToListAsync();
        }

        public async Task<int> CountSearchAsync(string roomId, MessageSearchQuery q)
        {
            if (_db.IsInMemory)
                return ApplyFilter(_db.Messages.Values, roomId, q).Count();
            return (int)await _col!.CountDocumentsAsync(BuildMongoFilter(roomId, q));
        }

        public async Task<Message?> EditAsync(string id, string senderId, string content)
        {
            var msg = await GetByIdAsync(id);
            if (msg is null || msg.SenderId != senderId) return null;
            msg.Content    = content.Trim();
            msg.IsEdited   = true;
            msg.UpdatedAt  = DateTime.UtcNow;
            if (_db.IsInMemory) { _db.Messages[id] = msg; return msg; }
            await _col!.UpdateOneAsync(m => m.Id == id,
                Builders<Message>.Update
                    .Set(m => m.Content,   msg.Content)
                    .Set(m => m.IsEdited,  true)
                    .Set(m => m.UpdatedAt, msg.UpdatedAt));
            return msg;
        }

        public async Task MarkReadAsync(string id, string userId)
        {
            var msg = await GetByIdAsync(id);
            if (msg is null || msg.ReadBy.Contains(userId)) return;
            msg.ReadBy.Add(userId);
            if (_db.IsInMemory) { _db.Messages[id] = msg; return; }
            await _col!.UpdateOneAsync(m => m.Id == id,
                Builders<Message>.Update.AddToSet(m => m.ReadBy, userId));
        }

        public async Task<int> GetUnreadCountAsync(string roomId, string userId)
        {
            if (_db.IsInMemory)
                return _db.Messages.Values.Count(m =>
                    m.RoomId == roomId && !m.IsDeleted &&
                    !m.ReadBy.Contains(userId) && m.SenderId != userId);

            return (int)await _col!.CountDocumentsAsync(m =>
                m.RoomId == roomId && !m.IsDeleted &&
                !m.ReadBy.Contains(userId) && m.SenderId != userId);
        }

        public async Task AddReactionAsync(string id, string emoji, string userId)
        {
            var msg = await GetByIdAsync(id);
            if (msg is null) return;
            if (!msg.Reactions.ContainsKey(emoji)) msg.Reactions[emoji] = new();
            if (!msg.Reactions[emoji].Contains(userId)) msg.Reactions[emoji].Add(userId);
            if (_db.IsInMemory) { _db.Messages[id] = msg; return; }
            await _col!.UpdateOneAsync(m => m.Id == id,
                Builders<Message>.Update.AddToSet($"reactions.{emoji}", userId));
        }

        public async Task RemoveReactionAsync(string id, string emoji, string userId)
        {
            var msg = await GetByIdAsync(id);
            if (msg is null) return;
            if (msg.Reactions.TryGetValue(emoji, out var users)) users.Remove(userId);
            if (_db.IsInMemory) { _db.Messages[id] = msg; return; }
            await _col!.UpdateOneAsync(m => m.Id == id,
                Builders<Message>.Update.Pull($"reactions.{emoji}", userId));
        }

        // ── Helpers ──────────────────────────────────────────────

        private static IEnumerable<Message> ApplyFilter(
            IEnumerable<Message> src, string roomId, MessageSearchQuery q)
        {
            src = src.Where(m => m.RoomId == roomId && !m.IsDeleted);
            if (!string.IsNullOrWhiteSpace(q.Q))
                src = src.Where(m => m.Content.Contains(q.Q, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(q.SenderId))
                src = src.Where(m => m.SenderId == q.SenderId);
            if (!string.IsNullOrWhiteSpace(q.Tag))
                src = src.Where(m => m.Tags.Any(t => t.Equals(q.Tag, StringComparison.OrdinalIgnoreCase)));
            if (q.Type.HasValue)
                src = src.Where(m => m.Type == q.Type.Value);
            if (q.From.HasValue) src = src.Where(m => m.Timestamp >= q.From.Value);
            if (q.To.HasValue)   src = src.Where(m => m.Timestamp <= q.To.Value);
            return src.OrderByDescending(m => m.Timestamp);
        }

        private static FilterDefinition<Message> BuildMongoFilter(string roomId, MessageSearchQuery q)
        {
            var b = Builders<Message>.Filter;
            var filters = new List<FilterDefinition<Message>>
            {
                b.Eq(m => m.RoomId,    roomId),
                b.Eq(m => m.IsDeleted, false)
            };
            if (!string.IsNullOrWhiteSpace(q.Q))
                filters.Add(b.Regex(m => m.Content, new MongoDB.Bson.BsonRegularExpression(q.Q, "i")));
            if (!string.IsNullOrWhiteSpace(q.SenderId))
                filters.Add(b.Eq(m => m.SenderId, q.SenderId));
            if (!string.IsNullOrWhiteSpace(q.Tag))
                filters.Add(b.AnyEq(m => m.Tags, q.Tag));
            if (q.Type.HasValue)
                filters.Add(b.Eq(m => m.Type, q.Type.Value));
            if (q.From.HasValue) filters.Add(b.Gte(m => m.Timestamp, q.From.Value));
            if (q.To.HasValue)   filters.Add(b.Lte(m => m.Timestamp, q.To.Value));
            return b.And(filters);
        }
    }
}
