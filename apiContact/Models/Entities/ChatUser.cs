using apiContact.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace apiContact.Models.Entities
{
    public class ChatUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string Username    { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email       { get; set; } = string.Empty;
        public string AvatarUrl   { get; set; } = string.Empty;

        // Role — stored as string for JWT/Authorize(Roles) compatibility
        public string   Role      { get; set; } = nameof(UserRole.User);
        public UserRole RoleEnum  => Enum.TryParse<UserRole>(Role, true, out var r) ? r : UserRole.User;

        // Status
        public UserStatus Status { get; set; } = UserStatus.Offline;
        // Keep IsOnline for backward compat / SignalR presence
        public bool IsOnline
        {
            get => Status == UserStatus.Online;
            set => Status = value ? UserStatus.Online : UserStatus.Offline;
        }

        // Identity
        public string    PasswordHash       { get; set; } = string.Empty;
        public string?   RefreshToken       { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }

        // Timestamps
        public DateTime LastSeen  { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
