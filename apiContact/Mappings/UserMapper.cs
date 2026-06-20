using apiContact.Models.Dtos;
using apiContact.Models.Entities;
using apiContact.Models.Enums;

namespace apiContact.Mappings
{
    // ── Response records ─────────────────────────────────────────────────────
    // Typed shapes sent to callers. Controllers use these instead of anonymous
    // objects so the mapping logic lives in exactly one place.

    /// <summary>Full profile — returned to the owner and admins.</summary>
    public sealed record UserProfileDto(
        string   Id,
        string   Username,
        string   DisplayName,
        string   Email,
        string   AvatarUrl,
        string   Role,
        string   Status,
        bool     IsOnline,
        DateTime LastSeen,
        DateTime CreatedAt
    );

    /// <summary>Public profile — returned to peers (no email).</summary>
    public sealed record UserPublicProfileDto(
        string Id,
        string Username,
        string DisplayName,
        string AvatarUrl,
        string Role,
        string Status,
        bool   IsOnline
    );

    // ── Mapper ───────────────────────────────────────────────────────────────
    public static class UserMapper
    {
        // ── Entity → Response ────────────────────────────────────────────────

        public static UserProfileDto ToProfile(ChatUser u) => new(
            u.Id, u.Username, u.DisplayName,
            u.Email, u.AvatarUrl, u.Role,
            u.Status.ToString(), u.IsOnline,
            u.LastSeen, u.CreatedAt
        );

        public static UserPublicProfileDto ToPublicProfile(ChatUser u) => new(
            u.Id, u.Username, u.DisplayName,
            u.AvatarUrl, u.Role,
            u.Status.ToString(), u.IsOnline
        );

        // ── DTO → Entity ─────────────────────────────────────────────────────

        /// <summary>
        /// Builds a new <see cref="ChatUser"/> from a create DTO.
        /// Id and CreatedAt come from <see cref="BaseEntity"/> defaults (GUID / UtcNow).
        /// Caller must hash <paramref name="passwordHash"/> before passing it in.
        /// </summary>
        public static ChatUser FromCreateDto(CreateUserDto dto, string passwordHash) => new()
        {
            Username     = dto.Username.Trim().ToLowerInvariant(),
            DisplayName  = string.IsNullOrWhiteSpace(dto.DisplayName)
                               ? dto.Username.Trim()
                               : dto.DisplayName.Trim(),
            Email        = dto.Email.Trim().ToLowerInvariant(),
            AvatarUrl    = dto.AvatarUrl ?? string.Empty,
            Role         = IsValidRole(dto.Role) ? dto.Role : nameof(UserRole.User),
            PasswordHash = passwordHash,
        };

        /// <summary>
        /// Builds a new <see cref="ChatUser"/> from a register DTO.
        /// Password hashing is the caller's responsibility.
        /// </summary>
        public static ChatUser FromRegisterDto(RegisterDto dto, string passwordHash) => new()
        {
            Username     = dto.Username.Trim().ToLowerInvariant(),
            DisplayName  = string.IsNullOrWhiteSpace(dto.DisplayName)
                               ? dto.Username.Trim()
                               : dto.DisplayName.Trim(),
            Email        = dto.Email.Trim().ToLowerInvariant(),
            AvatarUrl    = dto.AvatarUrl ?? string.Empty,
            Role         = nameof(UserRole.User),
            PasswordHash = passwordHash,
        };

        // ── Patch ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a partial update from <paramref name="dto"/> to an existing entity.
        /// Only non-null fields are written. UpdatedAt is always refreshed.
        /// </summary>
        public static void ApplyUpdate(ChatUser user, UpdateUserDto dto)
        {
            if (dto.DisplayName is not null)
                user.DisplayName = dto.DisplayName.Trim();

            if (dto.AvatarUrl is not null)
                user.AvatarUrl = dto.AvatarUrl;

            if (dto.IsOnline.HasValue)
            {
                user.Status   = dto.IsOnline.Value ? UserStatus.Online : UserStatus.Offline;
                user.LastSeen = DateTime.UtcNow;
            }

            user.UpdatedAt = DateTime.UtcNow;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool IsValidRole(string? role) =>
            !string.IsNullOrWhiteSpace(role) &&
            Enum.TryParse<UserRole>(role, ignoreCase: true, out _);
    }
}
