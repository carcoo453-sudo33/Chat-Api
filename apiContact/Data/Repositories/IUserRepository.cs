using apiContact.Models.Dtos;
using apiContact.Models.Entities;

namespace apiContact.Data.Repositories
{
    public interface IUserRepository : IRepository<ChatUser>
    {
        Task<ChatUser?> GetByUsernameAsync(string username);
        Task<ChatUser?> GetByEmailAsync(string email);
        Task<List<ChatUser>> GetOnlineUsersAsync();
        Task             SetStatusAsync(string id, bool isOnline);
        Task             SaveRefreshTokenAsync(string id, string? token, DateTime? expiry);
        Task<bool>       ChangePasswordAsync(string id, string newPasswordHash);
        Task<List<ChatUser>> SearchAsync(string query, int skip, int take);
    }
}
