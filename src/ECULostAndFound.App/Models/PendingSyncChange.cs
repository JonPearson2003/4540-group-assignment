using SQLite;

namespace ECULostAndFound.App.Models;

public class PendingSyncChange
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string ChangeId { get; set; } = string.Empty;

    public int RecordId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string DeviceUserId { get; set; } = string.Empty;
    public bool IsSynced { get; set; } 
}