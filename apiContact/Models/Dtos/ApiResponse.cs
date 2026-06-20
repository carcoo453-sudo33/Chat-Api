namespace apiContact.Models.Dtos
{
    public class ApiResponse<T>
    {
        public bool    Success     { get; set; }
        public string  Message     { get; set; } = string.Empty;
        public T?      Data        { get; set; }
        public int?    Total       { get; set; }

        // Pagination metadata (populated on paged responses)
        public int?  Page       { get; set; }
        public int?  PageSize   { get; set; }
        public int?  TotalPages { get; set; }
        public bool? HasNext    { get; set; }
        public bool? HasPrevious{ get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success", int? total = null)
            => new() { Success = true, Data = data, Message = message, Total = total };

        public static ApiResponse<T> Fail(string message)
            => new() { Success = false, Message = message };
    }

    /// <summary>Paged wrapper — use this instead of ApiResponse&lt;T&gt;.Paged() to avoid cast issues</summary>
    public static class PagedApiResponse
    {
        public static object From<T>(PagedResult<T> paged, string message = "Success") => new
        {
            success     = true,
            message,
            data        = paged.Items,
            total       = paged.Total,
            page        = paged.Page,
            pageSize    = paged.PageSize,
            totalPages  = paged.TotalPages,
            hasNext     = paged.HasNext,
            hasPrevious = paged.HasPrevious
        };
    }
}
