using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Utilities;
using MongoDB.Bson;

namespace apiContact.Services
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _uow;
        public RoomService(IUnitOfWork uow) => _uow = uow;

        public Task<List<ChatRoom>> GetAllAsync()                        => _uow.Rooms.GetAllAsync();
        public Task<List<ChatRoom>> GetByUserAsync(string userId)        => _uow.Rooms.GetByUserAsync(userId);
        public Task<ChatRoom?> GetByIdAsync(string id)                   => _uow.Rooms.GetByIdAsync(id);

        public async Task<ChatRoom> CreateAsync(CreateRoomDto dto)
        {
            if (!dto.MemberIds.Contains(dto.CreatedBy))
                dto.MemberIds.Add(dto.CreatedBy);

            var allSlugs = (await _uow.Rooms.GetAllAsync()).Select(r => r.Slug);
            var slug     = SlugHelper.Uniquify(SlugHelper.Generate(dto.Name), allSlugs);

            var room = new ChatRoom
            {
                Id          = ObjectId.GenerateNewId().ToString(),
                Name        = dto.Name,
                Slug        = slug,
                Description = dto.Description,
                Category    = dto.Category,
                Tags        = dto.Tags,
                Type        = dto.Type,
                IsPrivate   = dto.IsPrivate,
                MemberIds   = dto.MemberIds,
                CreatedBy   = dto.CreatedBy,
                CreatedAt   = DateTime.UtcNow
            };
            return await _uow.Rooms.AddAsync(room);
        }

        public Task<bool> AddMemberAsync(string roomId, string userId)   => _uow.Rooms.AddMemberAsync(roomId, userId);
        public Task<bool> RemoveMemberAsync(string roomId, string userId) => _uow.Rooms.RemoveMemberAsync(roomId, userId);
        public Task<bool> DeleteAsync(string id)                          => _uow.Rooms.DeleteAsync(id);
        public Task UpdateLastMessageAsync(string roomId, string preview) => _uow.Rooms.UpdateLastMessageAsync(roomId, preview);
    }
}
