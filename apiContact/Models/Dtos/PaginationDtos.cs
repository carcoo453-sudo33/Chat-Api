namespace apiContact.Models.Dtos
{
    // ── Inbound ────────────────────────────────────────────────
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

    public class RoomSearchQuery : PagedQuery
    {
        public string? Q        { get; set; }   // full-text search
        public string? Category { get; set; }
        public string? Tag      { get; set; }
        public string? Type     { get; set; }   // "direct" | "group" | "channel"
        public string? Sort     { get; set; } = "activity"; // "activity" | "name" | "created"
    }

    public class MessageSearchQuery : PagedQuery
    {
        public string? Q        { get; set; }
        public string? Tag      { get; set; }
        public string? SenderId { get; set; }
        public string? Type     { get; set; }   // "text" | "image" | "file"
        public DateTime? From   { get; set; }
        public DateTime? To     { get; set; }
    }

    // ── Outbound ───────────────────────────────────────────────
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items       { get; init; } = Array.Empty<T>();
        public int              Total       { get; init; }
        public int              Page        { get; init; }
        public int              PageSize    { get; init; }
        public int              TotalPages  => (int)Math.Ceiling((double)Total / PageSize);
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
    }
}
