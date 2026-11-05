namespace ECULostAndFound.App.Models;

public class PushChangeRequest
{
    public string ChangeId { get; set; } = string.Empty;

    public int RecordId { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public DateTime LastModified { get; set; }

    public string DeviceUserId { get; set; } = string.Empty;

    public LostAndFoundItem? Item { get; set; }
}