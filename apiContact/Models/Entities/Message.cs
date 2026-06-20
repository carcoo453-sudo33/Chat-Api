using apiContact.Models.Enums;

namespace apiContact.Models.Entities
{
    /// <summary>
    /// A single chat message. Extends BaseEntity for GUID Id, timestamps, and
    /// a consistent soft-delete contract (IsDeleted / DeletedAt / DeletedBy).
    /// Soft-deleted messages have their content replaced with "[Message deleted]"
    /// but the document is preserved for audit / read-receipt integrity.
    /// </summary>
    public class Message : BaseEntity
    {
        public string      RoomId     { get; set; } = string.Empty;
        public string      SenderId   { get; set; } = string.Empty;
        public string      SenderName { get; set; } = string.Empty;
        public string      Content    { get; set; } = string.Empty;
        public MessageType Type       { get; set; } = MessageType.Text;

        // File attachment (when Type = Image or File)
        public string?   FileUrl         { get; set; }
        public string?   FileName        { get; set; }
        public long?     FileSize        { get; set; }
        public FileType? AttachmentType  { get; set; }

        // Searchable labels
        public List<string> Tags { get; set; } = new();

        // Reactions: emoji → list of userIds
        public Dictionary<string, List<string>> Reactions { get; set; } = new();

        // Edit state (IsDeleted comes from BaseEntity)
        public bool IsEdited { get; set; } = false;

        // Read receipts
        public List<string> ReadBy { get; set; } = new();

        // Convenience: Timestamp aligns with BaseEntity.CreatedAt but kept
        // as a separate field for backward compatibility with existing clients.
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
