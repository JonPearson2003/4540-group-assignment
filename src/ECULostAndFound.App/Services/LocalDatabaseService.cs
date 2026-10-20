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
    
    public void CreateItem(LostAndFoundItem item)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<LostAndFoundItem>("items");
        var now = DateTime.UtcNow;
        item.DateReported = now;
        item.LastUpdated = now;
        collection.Insert(item);
    }

    public bool UpdateItem(LostAndFoundItem item)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<LostAndFoundItem>("items");
        item.LastUpdated = DateTime.UtcNow;
        return collection.Update(item);
    }

    public bool DeleteItem(string id)
    {
        using var db = new LiteDatabase(_databasePath);
        var collection = db.GetCollection<LostAndFoundItem>("items");
        return collection.Delete(id);
    }
}