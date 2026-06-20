using apiContact.Models.Enums;

namespace apiContact.Models.Entities
{
    /// <summary>
    /// Chat room (Direct / Group / Channel). Extends BaseEntity for GUID Id,
    /// lifecycle timestamps, and soft-delete. Soft-delete replaces hard-delete
    /// so message history is preserved.
    /// </summary>
    public class ChatRoom : BaseEntity
    {
        public string   Name        { get; set; } = string.Empty;

        /// <summary>URL-safe slug, e.g. "engineering-team". Generated from Name via SlugHelper.</summary>
        public string   Slug        { get; set; } = string.Empty;

        public string   Description { get; set; } = string.Empty;

        /// <summary>Organisational category, e.g. "engineering", "hr", "general"</summary>
        public string   Category    { get; set; } = string.Empty;

        /// <summary>Searchable freeform labels.</summary>
        public List<string> Tags    { get; set; } = new();

        public RoomType Type        { get; set; } = RoomType.Group;

        /// <summary>
        /// Archived rooms are read-only (no new messages allowed).
        /// Different from IsDeleted — archived rooms remain visible.
        /// </summary>
        public bool IsArchived      { get; set; } = false;

        public bool IsPrivate       { get; set; } = false;

        public List<string> MemberIds { get; set; } = new();
        public string       CreatedBy { get; set; } = string.Empty;

        // Denormalized preview — avoids an extra query for the rooms list
        public string?   LastMessagePreview { get; set; }
        public DateTime? LastMessageAt      { get; set; }
    }
}
