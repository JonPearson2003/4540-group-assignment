namespace ECULostAndFound.App.Models;

public class ServerChangeDto
{
    public long Version { get; set; }
    public LostAndFoundItem Item { get; set; } = new();
}
