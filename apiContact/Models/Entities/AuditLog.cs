namespace apiContact.Models.Entities
{
    /// <summary>
    /// Immutable record of a security-relevant action. Extends BaseEntity for
    /// GUID Id and CreatedAt. IsDeleted / UpdatedAt are inherited but should
    /// never be set — audit logs are append-only by convention.
    /// </summary>
    public class AuditLog : BaseEntity
    {
        /// <summary>Dot-notated action key, e.g. "auth.login", "file.delete"</summary>
        public string  Action       { get; set; } = string.Empty;

        public string? UserId       { get; set; }
        public string? Username     { get; set; }

        /// <summary>Primary resource affected (roomId, messageId, fileName …)</summary>
        public string? ResourceId   { get; set; }

        /// <summary>Type label of that resource, e.g. "Room", "Message", "File"</summary>
        public string? ResourceType { get; set; }

        public string? IpAddress    { get; set; }
        public string? UserAgent    { get; set; }

        /// <summary>True when the action completed successfully; false on denied / failed.</summary>
        public bool    Success      { get; set; } = true;

        /// <summary>Optional sanitised context — no PII, no secrets.</summary>
        public string? Details      { get; set; }

        public DateTime Timestamp   { get; set; } = DateTime.UtcNow;
    }
}
