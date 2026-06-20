using apiContact.Models.Dtos;
using apiContact.Models.Entities;

namespace apiContact.Data.Repositories
{
    public interface IRoomRepository : IRepository<ChatRoom>
    {
        Task<ChatRoom?>      GetBySlugAsync(string slug);
        Task<List<ChatRoom>> GetByUserAsync(string userId);
        Task<List<ChatRoom>> SearchAsync(RoomSearchQuery q);
        Task<int>            CountSearchAsync(RoomSearchQuery q);
        Task<bool>           AddMemberAsync(string roomId, string userId);
        Task<bool>           RemoveMemberAsync(string roomId, string userId);
        Task                 UpdateLastMessageAsync(string roomId, string preview);
        Task<bool>           SlugExistsAsync(string slug);
    }
}
