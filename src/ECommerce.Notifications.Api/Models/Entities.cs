namespace ECommerce.Notifications.Api.Models;

/// <summary>
/// Represents an email log entry.
/// </summary>
public class EmailLog
{
    public int Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Email status enum.
/// </summary>
public enum EmailStatus
{
    Pending,
    Sent,
    Failed
}
