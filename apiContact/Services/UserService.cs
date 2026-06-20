using apiContact.Data.Repositories;
using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Models.Enums;
using MongoDB.Bson;

namespace apiContact.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;
        public UserService(IUnitOfWork uow) => _uow = uow;

        public Task<List<ChatUser>> GetAllAsync()                  => _uow.Users.GetAllAsync();
        public Task<ChatUser?> GetByIdAsync(string id)             => _uow.Users.GetByIdAsync(id);
        public Task<ChatUser?> GetByUsernameAsync(string username) => _uow.Users.GetByUsernameAsync(username);
        public Task<ChatUser?> GetByEmailAsync(string email)       => _uow.Users.GetByEmailAsync(email);

        public async Task<ChatUser> CreateAsync(CreateUserDto dto)
        {
            var user = new ChatUser
            {
                Id           = ObjectId.GenerateNewId().ToString(),
                Username     = dto.Username.Trim().ToLower(),
                DisplayName  = string.IsNullOrWhiteSpace(dto.DisplayName) ? dto.Username : dto.DisplayName,
                Email        = dto.Email.Trim().ToLower(),
                AvatarUrl    = dto.AvatarUrl,
                Role         = dto.Role,
                PasswordHash = dto.Password,
                CreatedAt    = DateTime.UtcNow
            };
            return await _uow.Users.AddAsync(user);
        }

        public async Task<ChatUser?> UpdateAsync(string id, UpdateUserDto dto)
        {
            var user = await _uow.Users.GetByIdAsync(id);
            if (user is null) return null;
            if (dto.DisplayName is not null) user.DisplayName = dto.DisplayName;
            if (dto.AvatarUrl   is not null) user.AvatarUrl   = dto.AvatarUrl;
            if (dto.IsOnline.HasValue)
            {
                user.Status   = dto.IsOnline.Value ? UserStatus.Online : UserStatus.Offline;
                user.LastSeen = DateTime.UtcNow;
            }
            return await _uow.Users.UpdateAsync(user);
        }

        public Task<bool> ChangePasswordAsync(string id, string newPasswordHash)
            => _uow.Users.ChangePasswordAsync(id, newPasswordHash);

        public Task SaveRefreshTokenAsync(string id, string? token, DateTime? expiry)
            => _uow.Users.SaveRefreshTokenAsync(id, token, expiry);

        public Task<bool> DeleteAsync(string id) => _uow.Users.DeleteAsync(id);

        public Task SetOnlineAsync(string id, bool isOnline)
            => _uow.Users.SetStatusAsync(id, isOnline ? UserStatus.Online : UserStatus.Offline);

        public Task SetStatusAsync(string id, UserStatus status)
            => _uow.Users.SetStatusAsync(id, status);
    }
}
