using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;
using SQLite;

namespace ECULostAndFound.App.Models;

public class LostAndFoundItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; } = 0;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime DateFound { get; set; } = DateTime.Today;

    public string CollectionLocation { get; set; } = string.Empty;

    public string CollectionDetails { get; set; } = string.Empty;

    public string PhotoData { get; set; } = string.Empty;

    public string ReportType { get; set; } = string.Empty;

    public string Status { get; set; } = "Open";

    public DateTime DateReported { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;

    public bool IsClaimed { get; set; }

    public bool IsDeleted { get; set; }

    public long ServerVersion { get; set; }

    [Ignore, JsonIgnore]
    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoData);

    [Ignore, JsonIgnore]
    public bool HasCollectionLocation =>
        !string.IsNullOrWhiteSpace(CollectionLocation);

    [Ignore, JsonIgnore]
    public bool HasCollectionDetails =>
        !string.IsNullOrWhiteSpace(CollectionDetails);

    [Ignore, JsonIgnore]
    public ImageSource? PhotoImageSource =>
        HasPhoto
            ? ImageSource.FromStream(
                () => new MemoryStream(Convert.FromBase64String(PhotoData)))
            : null;
}