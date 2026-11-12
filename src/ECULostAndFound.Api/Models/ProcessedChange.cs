namespace ECULostAndFound.Api.Models;

public class ProcessedChange
{
    public long Id { get; set; } // Server-assigned processing order
    public string ChangeId { get; set; } = string.Empty;
    public int RecordId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
    public string ItemJson { get; set; } = string.Empty;
}