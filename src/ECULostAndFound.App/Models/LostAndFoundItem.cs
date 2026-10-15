namespace ECULostAndFound.App.Models;

public class LostAndFoundItem
{
    public int Id { get; set; } = 0;
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
}