namespace ECommerce.Notifications.Api.Models;

/// <summary>
/// DTO for email log response.
/// </summary>
public record EmailLogDto
{
    public int Id { get; init; }
    public string Recipient { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string BodyPreview { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? SentAt { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Paginated email logs response.
/// </summary>
public record PaginatedEmailLogsResponse
{
    public IEnumerable<EmailLogDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
