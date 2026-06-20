using System.Linq.Expressions;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Models.Enums;
using MongoDB.Driver;

namespace apiContact.Data.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly ChatDbContext _db;
        private readonly IMongoCollection<ChatRoom>? _col;

        public RoomRepository(ChatDbContext db)
        {
            _db  = db;
            _col = db.GetCollection<ChatRoom>("rooms");
        }

        // ── IRepository<ChatRoom> ───────────────────────────────

        public async Task<ChatRoom?> GetByIdAsync(string id)
        {
            if (_db.IsInMemory)
            {
                var r = _db.Rooms.GetValueOrDefault(id);
                return r is { IsDeleted: false } ? r : null;
            }
            return await _col!.Find(r => r.Id == id && !r.IsDeleted).FirstOrDefaultAsync();
        }

        public async Task<List<ChatRoom>> GetAllAsync()
        {
            if (_db.IsInMemory)
                return _db.Rooms.Values
                    .Where(r => !r.IsDeleted)
                    .OrderByDescending(r => r.LastMessageAt)
                    .ToList();

            return await _col!
                .Find(r => !r.IsDeleted)
                .SortByDescending(r => r.LastMessageAt)
                .ToListAsync();
        }

        public async Task<List<ChatRoom>> FindAsync(Expression<Func<ChatRoom, bool>> predicate)
        {
            if (_db.IsInMemory)
                return _db.Rooms.Values
                    .Where(r => !r.IsDeleted)
                    .Where(predicate.Compile())
                    .ToList();

            var notDeleted = Builders<ChatRoom>.Filter.Eq(r => r.IsDeleted, false);
            return await _col!.Find(notDeleted & predicate).ToListAsync();
        }

        public async Task<ChatRoom> AddAsync(ChatRoom entity)
        {
            if (_db.IsInMemory) { _db.Rooms[entity.Id] = entity; return entity; }
            await _col!.InsertOneAsync(entity);
            return entity;
        }

        public async Task<ChatRoom?> UpdateAsync(ChatRoom entity)
        {
            if (_db.IsInMemory) { _db.Rooms[entity.Id] = entity; return entity; }
            await _col!.ReplaceOneAsync(r => r.Id == entity.Id, entity);
            return entity;
        }

        /// <summary>
        /// Soft-delete: marks the room as deleted rather than removing the record.
        /// All messages in the room remain intact for audit purposes.
        /// </summary>
        public async Task<bool> DeleteAsync(string id)
        {
            var room = await GetByIdAsync(id);
            if (room is null) return false;

            room.SoftDelete();

            if (_db.IsInMemory) { _db.Rooms[id] = room; return true; }

            await _col!.UpdateOneAsync(r => r.Id == id,
                Builders<ChatRoom>.Update
                    .Set(r => r.IsDeleted, true)
                    .Set(r => r.DeletedAt, room.DeletedAt)
                    .Set(r => r.UpdatedAt, room.UpdatedAt));
            return true;
        }

        public async Task<int> CountAsync(Expression<Func<ChatRoom, bool>>? predicate = null)
        {
            if (_db.IsInMemory)
            {
                var src = _db.Rooms.Values.Where(r => !r.IsDeleted);
                return predicate is null ? src.Count() : src.Count(predicate.Compile());
            }
            var notDeleted = Builders<ChatRoom>.Filter.Eq(r => r.IsDeleted, false);
            var combined   = predicate is null ? notDeleted : notDeleted & predicate;
            return (int)await _col!.CountDocumentsAsync(combined);
        }

        public async Task<bool> ExistsAsync(Expression<Func<ChatRoom, bool>> predicate)
        {
            if (_db.IsInMemory)
                return _db.Rooms.Values.Where(r => !r.IsDeleted).Any(predicate.Compile());

            var notDeleted = Builders<ChatRoom>.Filter.Eq(r => r.IsDeleted, false);
            return await _col!.Find(notDeleted & predicate).AnyAsync();
        }

        // ── IRoomRepository ──────────────────────────────────────

        public async Task<ChatRoom?> GetBySlugAsync(string slug)
        {
            if (_db.IsInMemory)
                return _db.Rooms.Values.FirstOrDefault(r =>
                    !r.IsDeleted &&
                    r.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

            return await _col!
                .Find(r => !r.IsDeleted && r.Slug == slug)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ChatRoom>> GetByUserAsync(string userId)
        {
            if (_db.IsInMemory)
                return _db.Rooms.Values
                    .Where(r => !r.IsDeleted && r.MemberIds.Contains(userId))
                    .OrderByDescending(r => r.LastMessageAt)
                    .ToList();

            return await _col!
                .Find(r => !r.IsDeleted && r.MemberIds.Contains(userId))
                .SortByDescending(r => r.LastMessageAt)
                .ToListAsync();
        }

        public async Task<List<ChatRoom>> SearchAsync(RoomSearchQuery q)
        {
            q.Clamp();
            if (_db.IsInMemory)
                return ApplyRoomFilter(_db.Rooms.Values.Where(r => !r.IsDeleted), q)
                    .Skip(q.Skip).Take(q.PageSize).ToList();

            var filter = BuildMongoRoomFilter(q);
            var sort   = BuildMongoRoomSort(q);
            return await _col!.Find(filter).Sort(sort).Skip(q.Skip).Limit(q.PageSize).ToListAsync();
        }

        public async Task<int> CountSearchAsync(RoomSearchQuery q)
        {
            if (_db.IsInMemory)
                return ApplyRoomFilter(_db.Rooms.Values.Where(r => !r.IsDeleted), q).Count();

            return (int)await _col!.CountDocumentsAsync(BuildMongoRoomFilter(q));
        }

        public async Task<bool> AddMemberAsync(string roomId, string userId)
        {
            var room = await GetByIdAsync(roomId);
            if (room is null || room.MemberIds.Contains(userId)) return false;
            room.MemberIds.Add(userId);
            room.UpdatedAt = DateTime.UtcNow;
            if (_db.IsInMemory) { _db.Rooms[roomId] = room; return true; }
            await _col!.UpdateOneAsync(r => r.Id == roomId,
                Builders<ChatRoom>.Update
                    .AddToSet(r => r.MemberIds, userId)
                    .Set(r => r.UpdatedAt, room.UpdatedAt));
            return true;
        }

        public async Task<bool> RemoveMemberAsync(string roomId, string userId)
        {
            var room = await GetByIdAsync(roomId);
            if (room is null) return false;
            room.MemberIds.Remove(userId);
            room.UpdatedAt = DateTime.UtcNow;
            if (_db.IsInMemory) { _db.Rooms[roomId] = room; return true; }
            await _col!.UpdateOneAsync(r => r.Id == roomId,
                Builders<ChatRoom>.Update
                    .Pull(r => r.MemberIds, userId)
                    .Set(r => r.UpdatedAt, room.UpdatedAt));
            return true;
        }

        public async Task UpdateLastMessageAsync(string roomId, string preview)
        {
            var room = await GetByIdAsync(roomId);
            if (room is null) return;
            room.LastMessagePreview = preview;
            room.LastMessageAt      = DateTime.UtcNow;
            room.UpdatedAt          = DateTime.UtcNow;
            if (_db.IsInMemory) { _db.Rooms[roomId] = room; return; }
            await _col!.UpdateOneAsync(r => r.Id == roomId,
                Builders<ChatRoom>.Update
                    .Set(r => r.LastMessagePreview, preview)
                    .Set(r => r.LastMessageAt,      room.LastMessageAt)
                    .Set(r => r.UpdatedAt,          room.UpdatedAt));
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            if (_db.IsInMemory)
                return _db.Rooms.Values.Any(r => !r.IsDeleted && r.Slug == slug);

            return await _col!.Find(r => !r.IsDeleted && r.Slug == slug).AnyAsync();
        }

        public Task<List<string>> GetAllCategoriesAsync()
        {
            if (_db.IsInMemory)
            {
                var cats = _db.Rooms.Values
                    .Where(r => !r.IsDeleted && !string.IsNullOrWhiteSpace(r.Category))
                    .Select(r => r.Category)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList();
                return Task.FromResult(cats);
            }
            return _col!.Distinct<string>("Category",
                    Builders<ChatRoom>.Filter.And(
                        Builders<ChatRoom>.Filter.Eq(r => r.IsDeleted, false),
                        Builders<ChatRoom>.Filter.Ne(r => r.Category, string.Empty)))
                .ToListAsync();
        }

        public Task<List<string>> GetAllTagsAsync()
        {
            if (_db.IsInMemory)
            {
                var tags = _db.Rooms.Values
                    .Where(r => !r.IsDeleted)
                    .SelectMany(r => r.Tags)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t)
                    .ToList();
                return Task.FromResult(tags);
            }
            return _col!.Distinct<string>("Tags",
                    Builders<ChatRoom>.Filter.Eq(r => r.IsDeleted, false))
                .ToListAsync();
        }

        public async Task<List<string>> GetMemberIdsAsync(string roomId)
        {
            var room = await GetByIdAsync(roomId);
            return room?.MemberIds ?? new List<string>();
        }

        // ── Helpers ──────────────────────────────────────────────

        private static IEnumerable<ChatRoom> ApplyRoomFilter(
            IEnumerable<ChatRoom> src, RoomSearchQuery q)
        {
            if (!q.IncludeArchived)
                src = src.Where(r => !r.IsArchived);

            if (!string.IsNullOrWhiteSpace(q.Q))
                src = src.Where(r =>
                    r.Name.Contains(q.Q,        StringComparison.OrdinalIgnoreCase) ||
                    r.Description.Contains(q.Q, StringComparison.OrdinalIgnoreCase) ||
                    r.Tags.Any(t => t.Contains(q.Q, StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrWhiteSpace(q.Category))
                src = src.Where(r => r.Category.Equals(q.Category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(q.Tag))
                src = src.Where(r => r.Tags.Any(t => t.Equals(q.Tag, StringComparison.OrdinalIgnoreCase)));

            if (q.Type.HasValue)
                src = src.Where(r => r.Type == q.Type.Value);

            return (q.SortBy, q.Direction) switch
            {
                (RoomSortBy.Name,        SortOrder.Asc)  => src.OrderBy(r => r.Name),
                (RoomSortBy.Name,        SortOrder.Desc) => src.OrderByDescending(r => r.Name),
                (RoomSortBy.CreatedAt,   SortOrder.Asc)  => src.OrderBy(r => r.CreatedAt),
                (RoomSortBy.CreatedAt,   SortOrder.Desc) => src.OrderByDescending(r => r.CreatedAt),
                (RoomSortBy.MemberCount, SortOrder.Asc)  => src.OrderBy(r => r.MemberIds.Count),
                (RoomSortBy.MemberCount, SortOrder.Desc) => src.OrderByDescending(r => r.MemberIds.Count),
                (RoomSortBy.Activity,    SortOrder.Asc)  => src.OrderBy(r => r.LastMessageAt),
                _                                        => src.OrderByDescending(r => r.LastMessageAt),
            };
        }

        private static FilterDefinition<ChatRoom> BuildMongoRoomFilter(RoomSearchQuery q)
        {
            var b       = Builders<ChatRoom>.Filter;
            var filters = new List<FilterDefinition<ChatRoom>>
            {
                b.Eq(r => r.IsDeleted, false)
            };

            if (!q.IncludeArchived)
                filters.Add(b.Eq(r => r.IsArchived, false));

            if (!string.IsNullOrWhiteSpace(q.Q))
                filters.Add(b.Or(
                    b.Regex(r => r.Name,        new MongoDB.Bson.BsonRegularExpression(q.Q, "i")),
                    b.Regex(r => r.Description, new MongoDB.Bson.BsonRegularExpression(q.Q, "i")),
                    b.AnyEq(r => r.Tags,        q.Q)));

            if (!string.IsNullOrWhiteSpace(q.Category))
                filters.Add(b.Regex(r => r.Category,
                    new MongoDB.Bson.BsonRegularExpression($"^{q.Category}$", "i")));

            if (!string.IsNullOrWhiteSpace(q.Tag))
                filters.Add(b.AnyEq(r => r.Tags, q.Tag));

            if (q.Type.HasValue)
                filters.Add(b.Eq(r => r.Type, q.Type.Value));

            return b.And(filters);
        }

        private static SortDefinition<ChatRoom> BuildMongoRoomSort(RoomSearchQuery q) =>
            (q.SortBy, q.Direction) switch
            {
                (RoomSortBy.Name,        SortOrder.Asc)  => Builders<ChatRoom>.Sort.Ascending(r => r.Name),
                (RoomSortBy.Name,        SortOrder.Desc) => Builders<ChatRoom>.Sort.Descending(r => r.Name),
                (RoomSortBy.CreatedAt,   SortOrder.Asc)  => Builders<ChatRoom>.Sort.Ascending(r => r.CreatedAt),
                (RoomSortBy.CreatedAt,   SortOrder.Desc) => Builders<ChatRoom>.Sort.Descending(r => r.CreatedAt),
                (RoomSortBy.MemberCount, _)              => Builders<ChatRoom>.Sort.Descending("membersCount"),
                (RoomSortBy.Activity,    SortOrder.Asc)  => Builders<ChatRoom>.Sort.Ascending(r => r.LastMessageAt),
                _                                        => Builders<ChatRoom>.Sort.Descending(r => r.LastMessageAt),
            };
    }
}
