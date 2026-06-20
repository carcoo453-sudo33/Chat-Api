using apiContact.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace apiContact.Models.Entities
{
    public class Message
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        public string      RoomId     { get; set; } = string.Empty;
        public string      SenderId   { get; set; } = string.Empty;
        public string      SenderName { get; set; } = string.Empty;
        public string      Content    { get; set; } = string.Empty;
        public MessageType Type       { get; set; } = MessageType.Text;

        // File attachment (when Type = Image or File)
        public string? FileUrl  { get; set; }
        public string? FileName { get; set; }
        public long?   FileSize { get; set; }
        public FileType? AttachmentType { get; set; }

        // Labels
        public List<string> Tags { get; set; } = new();

        // Reactions: emoji → list of userIds
        public Dictionary<string, List<string>> Reactions { get; set; } = new();

        // State
        public bool         IsEdited  { get; set; } = false;
        public bool         IsDeleted { get; set; } = false;
        public List<string> ReadBy    { get; set; } = new();

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
