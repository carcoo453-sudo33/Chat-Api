using apiContact.Models.Enums;

namespace apiContact.Models.Entities
{
    /// <summary>
    /// Registered user account. Extends BaseEntity for GUID Id,
    /// lifecycle timestamps, and soft-delete support.
    /// </summary>
    public class ChatUser : BaseEntity
    {
        public string Username    { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
        public string AvatarUrl   { get; set; } = string.Empty;

        // Role — stored as string for JWT/Authorize(Roles) compatibility
        public string   Role     { get; set; } = nameof(UserRole.User);
        public UserRole RoleEnum => Enum.TryParse<UserRole>(Role, true, out var r) ? r : UserRole.User;

        // Presence
        public UserStatus Status  { get; set; } = UserStatus.Offline;
        public bool IsOnline
        {
            get => Status == UserStatus.Online;
            set => Status = value ? UserStatus.Online : UserStatus.Offline;
        }

        // Identity / session
        public string    PasswordHash       { get; set; } = string.Empty;
        public string?   RefreshToken       { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        // LastSeen is separate from UpdatedAt — updated on every presence change
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}
