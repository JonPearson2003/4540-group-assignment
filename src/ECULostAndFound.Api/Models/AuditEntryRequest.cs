namespace ECULostAndFound.Api.Models;

public class AuditEntryRequest
{
    public string ChangeId { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
