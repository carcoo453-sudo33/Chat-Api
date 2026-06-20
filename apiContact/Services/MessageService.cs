using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using MongoDB.Bson;

namespace apiContact.Services
{
    public class MessageService : IMessageService
    {
        private readonly IUnitOfWork _uow;
        public MessageService(IUnitOfWork uow) => _uow = uow;

        public Task<List<Message>> GetByRoomAsync(string roomId, int limit = 50, int skip = 0)
            => _uow.Messages.GetByRoomAsync(roomId, limit, skip);

        public Task<Message?> GetByIdAsync(string id)
            => _uow.Messages.GetByIdAsync(id);

        public async Task<Message> SendAsync(SendMessageDto dto, string senderName)
        {
            var msg = new Message
            {
                Id         = ObjectId.GenerateNewId().ToString(),
                RoomId     = dto.RoomId,
                SenderId   = dto.SenderId,
                SenderName = senderName,
                Content    = dto.Content,
                Type       = dto.Type,
                Tags       = dto.Tags,
                Timestamp  = DateTime.UtcNow
            };
            return await _uow.Messages.AddAsync(msg);
        }

        public Task<Message?> EditAsync(string id, string senderId, string content)
            => _uow.Messages.EditAsync(id, senderId, content);

        public Task<bool> DeleteAsync(string id, string senderId)
            => _uow.Messages.DeleteAsync(id);

        public Task MarkReadAsync(string id, string userId)
            => _uow.Messages.MarkReadAsync(id, userId);

        public Task<int> GetUnreadCountAsync(string roomId, string userId)
            => _uow.Messages.GetUnreadCountAsync(roomId, userId);
    }
}
