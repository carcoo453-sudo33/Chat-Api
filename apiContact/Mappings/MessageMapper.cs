using apiContact.Models.Dtos;
using apiContact.Models.Entities;

namespace apiContact.Mappings
{
    /// <summary>
    /// All entity ↔ DTO conversions for <see cref="Message"/>.
    /// </summary>
    public static class MessageMapper
    {
        // ── DTO → Entity ─────────────────────────────────────────────────────

        /// <summary>
        /// Constructs a new <see cref="Message"/> from a send DTO.
        /// Id and CreatedAt come from <see cref="BaseEntity"/> defaults (GUID / UtcNow).
        /// <paramref name="senderName"/> should be resolved from the JWT claim by the caller.
        /// </summary>
        public static Message FromSendDto(SendMessageDto dto, string senderName) => new()
        {
            RoomId     = dto.RoomId.Trim(),
            SenderId   = dto.SenderId.Trim(),
            SenderName = senderName,
            Content    = dto.Content.Trim(),
            Type       = dto.Type,
            Tags       = NormaliseTags(dto.Tags),
            Timestamp  = DateTime.UtcNow,
        };

        // ── Helpers ──────────────────────────────────────────────────────────

        private static List<string> NormaliseTags(IEnumerable<string>? tags) =>
            (tags ?? Enumerable.Empty<string>())
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
    }
}
