using System.Text.Json;
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

        await EnsureColumnExistsAsync(
            "LostAndFoundItem",
            "ServerVersion",
            "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnExistsAsync(
            "PendingSyncChange",
            "ItemJson",
            "TEXT NULL");
        await EnsureColumnExistsAsync(
            "DownloadedChange",
            "ServerVersion",
            "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnExistsAsync(
            "ChangesTable",
            "ChangeId",
            "TEXT NOT NULL DEFAULT ''");
        await EnsureColumnExistsAsync(
            "ChangesTable",
            "ActorId",
            "TEXT NOT NULL DEFAULT 'Unknown'");
        await EnsureColumnExistsAsync(
            "ChangesTable",
            "Source",
            "TEXT NOT NULL DEFAULT 'Device'");
        await EnsureColumnExistsAsync(
            "ChangesTable",
            "IsSynced",
            "INTEGER NOT NULL DEFAULT 0");

        var auditEntriesWithoutId = await database
            .Table<ChangesTable>()
            .Where(change => change.ChangeId == null || change.ChangeId == "")
            .ToListAsync();

        foreach (var auditEntry in auditEntriesWithoutId)
        {
            auditEntry.ChangeId = Guid.NewGuid().ToString();
            await database.UpdateAsync(auditEntry);
        }

        var pendingChangesWithoutSnapshot = await database
            .Table<PendingSyncChange>()
            .Where(change =>
                !change.IsSynced &&
                change.OperationType != "delete" &&
                change.ItemJson == null)
            .ToListAsync();

        foreach (var pendingChange in pendingChangesWithoutSnapshot)
        {
            var item = await database
                .Table<LostAndFoundItem>()
                .Where(existing => existing.Id == pendingChange.RecordId)
                .FirstOrDefaultAsync();

            if (item is null)
            {
                continue;
            }

            pendingChange.ItemJson = JsonSerializer.Serialize(item);
            await database.UpdateAsync(pendingChange);
        }
    }

    private async Task EnsureColumnExistsAsync(
        string tableName,
        string columnName,
        string columnDefinition)
    {
        var columns = await database.GetTableInfoAsync(tableName);

        if (columns.Any(column => column.Name == columnName))
        {
            return;
        }

        await database.ExecuteAsync(
            $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
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

        item.IsDeleted = true;
        item.LastUpdated = DateTime.UtcNow;

        return await database.UpdateAsync(item);
    }

    public async Task<int> SaveChangeAsync(
        ChangesTable change)
    {
        await initialization;

        if (string.IsNullOrWhiteSpace(change.ChangeId))
        {
            change.ChangeId = Guid.NewGuid().ToString();
        }

        return await database.InsertAsync(change);
    }

    public async Task<List<ChangesTable>> GetChangesAsync()
    {
        await initialization;

        return await database
            .Table<ChangesTable>()
            .OrderByDescending(change => change.Change_Id)
            .ToListAsync();
    }

    public async Task<List<ChangesTable>> GetUnsyncedAuditEntriesAsync()
    {
        await initialization;

        return await database
            .Table<ChangesTable>()
            .Where(change => !change.IsSynced)
            .OrderBy(change => change.Change_Id)
            .ToListAsync();
    }

    public async Task<int> MarkAuditEntrySyncedAsync(int id)
    {
        await initialization;

        var change = await database
            .Table<ChangesTable>()
            .Where(entry => entry.Change_Id == id)
            .FirstOrDefaultAsync();

        if (change is null)
        {
            return 0;
        }

        change.IsSynced = true;
        return await database.UpdateAsync(change);
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

    public async Task<int> ApplyServerItemAsync(LostAndFoundItem serverItem)
    {
        await initialization;

        var localItem = await database
            .Table<LostAndFoundItem>()
            .Where(item => item.Id == serverItem.Id)
            .FirstOrDefaultAsync();

        if (localItem is not null &&
            serverItem.ServerVersion <= localItem.ServerVersion)
        {
            return 0;
        }

        if (localItem is null)
        {
            return await database.InsertAsync(serverItem);
        }

        var hasPendingLocalChange = await database
            .Table<PendingSyncChange>()
            .Where(change =>
                change.RecordId == serverItem.Id &&
                !change.IsSynced)
            .FirstOrDefaultAsync() is not null;

        if (hasPendingLocalChange)
        {
            localItem.ServerVersion = serverItem.ServerVersion;
            return await database.UpdateAsync(localItem);
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
        localItem.IsDeleted = serverItem.IsDeleted;
        localItem.ServerVersion = serverItem.ServerVersion;

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

        localItem.IsDeleted = true;
        localItem.LastUpdated = DateTime.UtcNow;

        return await database.UpdateAsync(localItem);
    }

    public async Task StageDownloadedChangesAsync(
        IEnumerable<ServerChangeDto> changes)
    {
        await initialization;

        foreach (var change in changes)
        {
            var alreadyStaged = await database
                .Table<DownloadedChange>()
                .Where(staged =>
                    staged.ServerVersion == change.Version)
                .FirstOrDefaultAsync();

            if (alreadyStaged is not null)
            {
                continue;
            }

            var item = change.Item;
            var downloadedChange = new DownloadedChange
            {
                RecordId = item.Id,
                ServerVersion = change.Version,
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
                ServerVersion = change.ServerVersion,
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

    public async Task ClearDownloadedChangesAsync()
    {
        await initialization;
        await database.DeleteAllAsync<DownloadedChange>();
    }

    public async Task<int> SetServerVersionAsync(
        int itemId,
        long serverVersion)
    {
        await initialization;

        var item = await database
            .Table<LostAndFoundItem>()
            .Where(existing => existing.Id == itemId)
            .FirstOrDefaultAsync();

        if (item is null || item.ServerVersion >= serverVersion)
        {
            return 0;
        }

        item.ServerVersion = serverVersion;
        return await database.UpdateAsync(item);
    }
}