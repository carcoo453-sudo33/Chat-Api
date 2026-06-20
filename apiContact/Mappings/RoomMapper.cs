using apiContact.Models.Dtos;
using apiContact.Models.Entities;

namespace apiContact.Mappings
{
    /// <summary>
    /// All entity ↔ DTO conversions for <see cref="ChatRoom"/>.
    /// No anonymous objects, no inline projections in handlers or controllers.
    /// </summary>
    public static class RoomMapper
    {
        // ── DTO → Entity ─────────────────────────────────────────────────────

        /// <summary>
        /// Constructs a new <see cref="ChatRoom"/> from a create DTO.
        /// Id and CreatedAt come from <see cref="BaseEntity"/> defaults (GUID / UtcNow).
        /// The caller is responsible for generating a unique <paramref name="uniqueSlug"/>
        /// via <c>SlugHelper.Generate</c> + <c>SlugHelper.Uniquify</c>.
        /// The caller's id is added to MemberIds if not already present.
        /// </summary>
        public static ChatRoom FromCreateDto(
            CreateRoomDto dto,
            string        callerId,
            string        uniqueSlug) => new()
        {
            Name        = dto.Name.Trim(),
            Slug        = uniqueSlug,
            Description = dto.Description.Trim(),
            Category    = NormaliseLabel(dto.Category),
            Tags        = NormaliseLabels(dto.Tags),
            Type        = dto.Type,
            IsPrivate   = dto.IsPrivate,
            MemberIds   = dto.MemberIds
                            .Select(id => id.Trim())
                            .Where(id => id.Length > 0)
                            .Append(callerId)           // ensure creator is a member
                            .Distinct(StringComparer.Ordinal)
                            .ToList(),
            CreatedBy   = callerId,
        };

        // ── Patch ────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a partial update from <paramref name="dto"/> to an existing room.
        /// Pass <paramref name="newSlug"/> (already uniquified) when the name is changing;
        /// leave it <c>null</c> to keep the current slug.
        /// UpdatedAt is always refreshed.
        /// </summary>
        public static void ApplyUpdate(ChatRoom room, UpdateRoomDto dto, string? newSlug = null)
        {
            if (dto.Name is not null)
            {
                room.Name = dto.Name.Trim();
                if (newSlug is not null)
                    room.Slug = newSlug;
            }

            if (dto.Description is not null)
                room.Description = dto.Description.Trim();

            if (dto.Category is not null)
                room.Category = NormaliseLabel(dto.Category);

            if (dto.Tags is not null)
                room.Tags = NormaliseLabels(dto.Tags);

            if (dto.IsArchived.HasValue) room.IsArchived = dto.IsArchived.Value;
            if (dto.IsPrivate.HasValue)  room.IsPrivate  = dto.IsPrivate.Value;

            room.UpdatedAt = DateTime.UtcNow;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>Trims and lowercases a single label (category).</summary>
        private static string NormaliseLabel(string? raw) =>
            string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim().ToLowerInvariant();

        /// <summary>Trims, lowercases, deduplicates, and sorts a tag list.</summary>
        private static List<string> NormaliseLabels(IEnumerable<string>? tags) =>
            (tags ?? Enumerable.Empty<string>())
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(t => t)
                .ToList();
    }
}
