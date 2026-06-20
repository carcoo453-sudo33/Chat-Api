using apiContact.Models.Enums;

namespace apiContact.Models.Dtos
{
    // ── Base paged query ───────────────────────────────────────
    public class PagedQuery
    {
        public int Page     { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int Skip => (Page - 1) * PageSize;

        public void Clamp(int maxPageSize = 100)
        {
            Page     = Math.Max(1, Page);
            PageSize = Math.Clamp(PageSize, 1, maxPageSize);
        }
    }

    // ── Room search ────────────────────────────────────────────
    public class RoomSearchQuery : PagedQuery
    {
        /// <summary>Full-text search across name, description and tags</summary>
        public string? Q { get; set; }

        /// <summary>Filter by category name (case-insensitive)</summary>
        public string? Category { get; set; }

        /// <summary>Filter by a single tag (case-insensitive)</summary>
        public string? Tag { get; set; }

        /// <summary>Filter by room type</summary>
        public RoomType? Type { get; set; }

        /// <summary>Sort order</summary>
        public RoomSortBy SortBy { get; set; } = RoomSortBy.Activity;

        /// <summary>Sort direction</summary>
        public SortOrder Direction { get; set; } = SortOrder.Desc;

        /// <summary>Include archived rooms</summary>
        public bool IncludeArchived { get; set; } = false;
    }

    // ── Message search ─────────────────────────────────────────
    public class MessageSearchQuery : PagedQuery
    {
        /// <summary>Full-text search on content</summary>
        public string? Q { get; set; }

        /// <summary>Filter by tag</summary>
        public string? Tag { get; set; }

        /// <summary>Filter by sender user ID</summary>
        public string? SenderId { get; set; }

        /// <summary>Filter by message type</summary>
        public MessageType? Type { get; set; }

        /// <summary>Earliest timestamp (inclusive)</summary>
        public DateTime? From { get; set; }

        /// <summary>Latest timestamp (inclusive)</summary>
        public DateTime? To { get; set; }
    }

    // ── User search ────────────────────────────────────────────
    public class UserSearchQuery : PagedQuery
    {
        /// <summary>Full-text search across username, display name and email</summary>
        public string? Q { get; set; }

        /// <summary>Filter by role</summary>
        public UserRole? Role { get; set; }

        /// <summary>Only return online users</summary>
        public bool? OnlineOnly { get; set; }
    }

    // ── Outbound ───────────────────────────────────────────────
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items       { get; init; } = Array.Empty<T>();
        public int              Total       { get; init; }
        public int              Page        { get; init; }
        public int              PageSize    { get; init; }
        public int              TotalPages  => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
        public bool             HasPrevious => Page > 1;
        public bool             HasNext     => Page < TotalPages;

        public static PagedResult<T> From(
            IEnumerable<T> source, int total, int page, int pageSize)
            => new()
            {
                Items    = source.ToList(),
                Total    = total,
                Page     = page,
                PageSize = pageSize
            };

        public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
            => new() { Items = Array.Empty<T>(), Total = 0, Page = page, PageSize = pageSize };
    }
}
