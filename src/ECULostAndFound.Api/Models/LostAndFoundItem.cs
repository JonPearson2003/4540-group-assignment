namespace ECULostAndFound.Api.Models;

public class LostAndFoundItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string ReportType { get; set; } = string.Empty;

    public string Status { get; set; } = "Open";

    public DateTime DateReported { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;

    public bool IsClaimed { get; set; }

    // Week 4: lets other devices learn that this item was deleted.
    public bool IsDeleted { get; set; }

    public long ServerVersion { get; set; }
}