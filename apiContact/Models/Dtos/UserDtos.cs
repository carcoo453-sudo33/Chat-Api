using apiContact.Models.Enums;

namespace apiContact.Models.Dtos
{
    // ── Auth ──────────────────────────────────────────────────
    public class RegisterDto
    {
        public string Username    { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
        public string Password    { get; set; } = string.Empty;
        public string AvatarUrl   { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password        { get; set; } = string.Empty;
    }

    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword     { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string   AccessToken  { get; set; } = string.Empty;
        public string   RefreshToken { get; set; } = string.Empty;
        public string   UserId       { get; set; } = string.Empty;
        public string   Username     { get; set; } = string.Empty;
        public string   DisplayName  { get; set; } = string.Empty;
        public string   Role         { get; set; } = string.Empty;
        public DateTime ExpiresAt    { get; set; }
    }

    // ── User profile management ───────────────────────────────
    public class CreateUserDto
    {
        public string Username    { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
        public string Password    { get; set; } = string.Empty;
        public string AvatarUrl   { get; set; } = string.Empty;
        public string Role        { get; set; } = nameof(UserRole.User);
    }

    public class UpdateUserDto
    {
        public string? DisplayName { get; set; }
        public string? AvatarUrl   { get; set; }

        /// <summary>Deprecated — use SetStatusDto for explicit presence control</summary>
        public bool? IsOnline { get; set; }
    }

    /// <summary>Set a user's presence status explicitly</summary>
    public class SetStatusDto
    {
        public UserStatus Status { get; set; } = UserStatus.Online;
    }
}
