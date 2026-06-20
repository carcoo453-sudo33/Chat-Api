using apiContact.Models.Dtos;
using apiContact.Models.Entities;

namespace apiContact.Data.Repositories
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<List<Message>> GetByRoomAsync(string roomId, int limit, int skip);
        Task<List<Message>> SearchAsync(string roomId, MessageSearchQuery q);
        Task<int>           CountSearchAsync(string roomId, MessageSearchQuery q);
        Task<Message?>      EditAsync(string id, string senderId, string content);
        Task                MarkReadAsync(string id, string userId);
        Task<int>           GetUnreadCountAsync(string roomId, string userId);
        Task                AddReactionAsync(string id, string emoji, string userId);
        Task                RemoveReactionAsync(string id, string emoji, string userId);
    }
}
