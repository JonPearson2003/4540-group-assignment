namespace ECULostAndFound.App.Models;

public class PushChangeResponse
{
    public string ChangeId { get; set; } = string.Empty;
    public long ServerVersion { get; set; }
}
