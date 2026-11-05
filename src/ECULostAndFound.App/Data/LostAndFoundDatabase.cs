using ECULostAndFound.App.Models;
using Microsoft.Maui.Storage;
using SQLite;

namespace ECULostAndFound.App.Data;

public sealed class LostAndFoundDatabase
{
    private readonly SQLiteAsyncConnection database;
    private readonly Task initialization;

    public LostAndFoundDatabase()
    {
        var databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "lostandfound.db3");

        database = new SQLiteAsyncConnection(
            databasePath,
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache);

        initialization = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await database.CreateTableAsync<LostAndFoundItem>();
        await database.CreateTableAsync<ChangesTable>();
        await database.CreateTableAsync<PendingSyncChange>();
        await database.CreateTableAsync<DownloadedChange>();
    }

    public async Task<List<LostAndFoundItem>> GetLostAndFoundItemsAsync()
    {
        await initialization;

        return await database
            .Table<LostAndFoundItem>()
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.Id)
            .ToListAsync();
    }

    public async Task<LostAndFoundItem?> GetLostAndFoundItemByIdAsync(int id)
    {
        await initialization;

        return await database
            .Table<LostAndFoundItem>()
            .Where(item => item.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveLostAndFoundItemAsync(
        LostAndFoundItem item)
    {
        await initialization;

        if (item.Id == 0)
        {
            var now = DateTime.UtcNow;

            item.DateReported = now;
            item.LastUpdated = now;
            item.IsDeleted = false;

            return await database.InsertAsync(item);
        }

        item.LastUpdated = DateTime.UtcNow;
        item.IsDeleted = false;

        return await database.UpdateAsync(item);
    }

    public async Task<int> DeleteLostAndFoundItemAsync(
        LostAndFoundItem item)
    {
        await initialization;

        return await database.DeleteAsync(item);
    }

    public async Task<int> SaveChangeAsync(
        ChangesTable change)
    {
        await initialization;

        return await database.InsertAsync(change);
    }

    public async Task<int> QueuePendingChangeAsync(
        PendingSyncChange change)
    {
        await initialization;

        if (string.IsNullOrWhiteSpace(change.ChangeId))
        {
            change.ChangeId = Guid.NewGuid().ToString();
        }

        change.IsSynced = false;

        return await database.InsertAsync(change);
    }

    public async Task<List<PendingSyncChange>> GetPendingChangesAsync()
    {
        await initialization;

        return await database
            .Table<PendingSyncChange>()
            .Where(change => !change.IsSynced)
            .OrderBy(change => change.Id)
            .ToListAsync();
    }

    public async Task<int> MarkChangeSyncedAsync(int id)
    {
        await initialization;

        var change = await database
            .Table<PendingSyncChange>()
            .Where(change => change.Id == id)
            .FirstOrDefaultAsync();

        if (change is null)
        {
            return 0;
        }

        change.IsSynced = true;

        return await database.UpdateAsync(change);
    }

    public async Task<int> ApplyServerItemAsync(
        LostAndFoundItem serverItem)
    {
        await initialization;

        if (serverItem.IsDeleted)
        {
            return await ApplyServerDeleteAsync(serverItem.Id);
        }

        var localItem = await database
            .Table<LostAndFoundItem>()
            .Where(item => item.Id == serverItem.Id)
            .FirstOrDefaultAsync();

        if (localItem is null)
        {
            return await database.ExecuteAsync(
                @"INSERT INTO LostAndFoundItem
                (Id, Title, Description, Category, Location, ReportType, Status, DateReported, LastUpdated, UserId, IsClaimed, IsDeleted)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                serverItem.Id,
                serverItem.Title,
                serverItem.Description,
                serverItem.Category,
                serverItem.Location,
                serverItem.ReportType,
                serverItem.Status,
                serverItem.DateReported,
                serverItem.LastUpdated,
                serverItem.UserId,
                serverItem.IsClaimed,
                serverItem.IsDeleted);
        }

        if (serverItem.LastUpdated < localItem.LastUpdated)
        {
            return 0;
        }

        localItem.Title = serverItem.Title;
        localItem.Description = serverItem.Description;
        localItem.Category = serverItem.Category;
        localItem.Location = serverItem.Location;
        localItem.ReportType = serverItem.ReportType;
        localItem.Status = serverItem.Status;
        localItem.DateReported = serverItem.DateReported;
        localItem.LastUpdated = serverItem.LastUpdated;
        localItem.UserId = serverItem.UserId;
        localItem.IsClaimed = serverItem.IsClaimed;
        localItem.IsDeleted = false;

        return await database.UpdateAsync(localItem);
    }

    public async Task<int> ApplyServerDeleteAsync(int itemId)
    {
        await initialization;

        var localItem = await database
            .Table<LostAndFoundItem>()
            .Where(item => item.Id == itemId)
            .FirstOrDefaultAsync();

        if (localItem is null)
        {
            return 0;
        }

        return await database.DeleteAsync(localItem);
    }

    public async Task StageDownloadedChangesAsync(
        IEnumerable<LostAndFoundItem> items)
    {
        await initialization;

        await database.DeleteAllAsync<DownloadedChange>();

        foreach (var item in items)
        {
            var downloadedChange = new DownloadedChange
            {
                RecordId = item.Id,
                Title = item.Title,
                Description = item.Description,
                Category = item.Category,
                Location = item.Location,
                ReportType = item.ReportType,
                Status = item.Status,
                DateReported = item.DateReported,
                LastUpdated = item.LastUpdated,
                UserId = item.UserId,
                IsClaimed = item.IsClaimed,
                IsDeleted = item.IsDeleted
            };

            await database.InsertAsync(downloadedChange);
        }
    }

    public async Task<List<DownloadedChange>> GetDownloadedChangesAsync()
    {
        await initialization;

        return await database
            .Table<DownloadedChange>()
            .OrderBy(change => change.DownloadId)
            .ToListAsync();
    }

    public async Task<int> ProcessDownloadedChangesAsync()
    {
        await initialization;

        var downloadedChanges =
            await GetDownloadedChangesAsync();

        int processed = 0;

        foreach (var change in downloadedChanges)
        {
            var serverItem = new LostAndFoundItem
            {
                Id = change.RecordId,
                Title = change.Title,
                Description = change.Description,
                Category = change.Category,
                Location = change.Location,
                ReportType = change.ReportType,
                Status = change.Status,
                DateReported = change.DateReported,
                LastUpdated = change.LastUpdated,
                UserId = change.UserId,
                IsClaimed = change.IsClaimed,
                IsDeleted = change.IsDeleted
            };

            await ApplyServerItemAsync(serverItem);

            processed++;
        }

        await database.DeleteAllAsync<DownloadedChange>();

        return processed;
    }
}