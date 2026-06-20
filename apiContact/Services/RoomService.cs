using apiContact.Data.Repositories;
using apiContact.Mappings;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Utilities;

namespace apiContact.Services
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _uow;
        public RoomService(IUnitOfWork uow) => _uow = uow;

        public Task<List<ChatRoom>> GetAllAsync()                        => _uow.Rooms.GetAllAsync();
        public Task<List<ChatRoom>> GetByUserAsync(string userId)        => _uow.Rooms.GetByUserAsync(userId);
        public Task<ChatRoom?> GetByIdAsync(string id)                   => _uow.Rooms.GetByIdAsync(id);

        public async Task<ChatRoom> CreateAsync(CreateRoomDto dto, string callerId)
        {
            var slug = await ResolveUniqueSlugAsync(SlugHelper.Generate(dto.Name));
            var room = RoomMapper.FromCreateDto(dto, callerId, slug);
            return await _uow.Rooms.AddAsync(room);
        }

        public Task<bool> AddMemberAsync(string roomId, string userId)   => _uow.Rooms.AddMemberAsync(roomId, userId);
        public Task<bool> RemoveMemberAsync(string roomId, string userId) => _uow.Rooms.RemoveMemberAsync(roomId, userId);
        public Task<bool> DeleteAsync(string id)                          => _uow.Rooms.DeleteAsync(id);
        public Task UpdateLastMessageAsync(string roomId, string preview) => _uow.Rooms.UpdateLastMessageAsync(roomId, preview);

        private async Task<string> ResolveUniqueSlugAsync(string baseSlug)
        {
            var slug = baseSlug;
            if (!await _uow.Rooms.SlugExistsAsync(slug)) return slug;

            int counter = 2;
            do { slug = $"{baseSlug}-{counter++}"; }
            while (await _uow.Rooms.SlugExistsAsync(slug));

            return slug;
        }
    }
}
