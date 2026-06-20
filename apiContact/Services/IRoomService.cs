using apiContact.Models.Dtos;
using apiContact.Models.Entities;

namespace apiContact.Services
{
    public interface IRoomService
    {
        Task<List<ChatRoom>> GetAllAsync();
        Task<List<ChatRoom>> GetByUserAsync(string userId);
        Task<ChatRoom?> GetByIdAsync(string id);
        Task<ChatRoom> CreateAsync(CreateRoomDto dto, string callerId);
        Task<bool> AddMemberAsync(string roomId, string userId);
        Task<bool> RemoveMemberAsync(string roomId, string userId);
        Task<bool> DeleteAsync(string id);
        Task UpdateLastMessageAsync(string roomId, string preview);
    }
}
