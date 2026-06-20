using apiContact.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace apiContact.Models.Entities
{
    public class ChatRoom
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string   Name        { get; set; } = string.Empty;
        public string   Slug        { get; set; } = string.Empty;   // URL-safe e.g. "engineering-team"
        public string   Description { get; set; } = string.Empty;
        public string   Category    { get; set; } = string.Empty;   // e.g. "engineering", "hr", "general"
        public List<string> Tags    { get; set; } = new();          // searchable labels

        public RoomType Type        { get; set; } = RoomType.Group;
        public bool     IsArchived  { get; set; } = false;
        public bool     IsPrivate   { get; set; } = false;

        public List<string> MemberIds { get; set; } = new();
        public string       CreatedBy { get; set; } = string.Empty;
        public DateTime     CreatedAt { get; set; } = DateTime.UtcNow;

        // Denormalized preview
        public string?  LastMessagePreview { get; set; }
        public DateTime? LastMessageAt     { get; set; }
    }
}
