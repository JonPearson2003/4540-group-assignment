using LiteDB;
using ECULostAndFound.App.Models;

namespace ECULostAndFound.App.Services;

public class LocalDatabaseService
{
    private readonly string _databasePath;

    public LocalDatabaseService()
    {
        _databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "ECULostAndFound.db");
    }

    public List<LostAndFoundItem> GetItems()
    {
        using var db = new LiteDatabase(_databasePath);
        var collection =
            db.GetCollection<LostAndFoundItem>("items");
        return collection
            .FindAll()
            .ToList();
    }
}