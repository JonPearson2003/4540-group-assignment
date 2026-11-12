using SQLite;

namespace ECULostAndFound.App.Models;

public class ChangesTable
{
    [PrimaryKey, AutoIncrement]
    public int Change_Id { get; set; } = 0;
    [Indexed]
    public int Item_Id { get; set; } = 0;
    public string ChangeId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime ChangeDate { get; set; } = DateTime.UtcNow;
    public string ActorId { get; set; } = string.Empty;
    public string Source { get; set; } = "Device";
    public bool IsSynced { get; set; }
}