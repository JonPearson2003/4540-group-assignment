using SQLite;

namespace ECULostAndFound.App.Models;

public class DownloadedChange
{
    [PrimaryKey, AutoIncrement]
    public int DownloadId { get; set; }

    public int RecordId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string ReportType { get; set; } = string.Empty;

    public string Status { get; set; } = "Open";

    public DateTime DateReported { get; set; }

    public DateTime LastUpdated { get; set; }

    public string UserId { get; set; } = string.Empty;

    public bool IsClaimed { get; set; }

    public bool IsDeleted { get; set; }
}