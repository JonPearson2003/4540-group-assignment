using System.Globalization;
using System.Net.Http.Json;
using ECULostAndFound.App.Data;
using ECULostAndFound.App.Models;
using Microsoft.Maui.Storage;

namespace ECULostAndFound.App.Services;

public class SyncService
{
    private const string LastSyncKey = "LastSyncTime";
    private const string PendingPullTimeKey = "PendingPullTime";

    private readonly HttpClient httpClient;
    private readonly LostAndFoundDatabase database;

    public SyncService(
        HttpClient httpClient,
        LostAndFoundDatabase database)
    {
        this.httpClient = httpClient;
        this.database = database;
    }

    public DateTime GetLastSyncTime()
    {
        var storedValue = Preferences.Default.Get(
            LastSyncKey,
            string.Empty);

        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return DateTime.MinValue;
        }

        if (DateTime.TryParse(
            storedValue,
            null,
            DateTimeStyles.RoundtripKind,
            out var lastSync))
        {
            return lastSync;
        }

        return DateTime.MinValue;
    }

    public async Task<int> PushChangesAsync()
    {
        var pendingChanges =
            await database.GetPendingChangesAsync();

        int pushed = 0;

        foreach (var pendingChange in pendingChanges)
        {
            LostAndFoundItem? item = null;

            if (!string.Equals(
                pendingChange.OperationType,
                "delete",
                StringComparison.OrdinalIgnoreCase))
            {
                item = await database
                    .GetLostAndFoundItemByIdAsync(
                        pendingChange.RecordId);

                if (item is null)
                {
                    continue;
                }
            }

            var request = new PushChangeRequest
            {
                ChangeId = pendingChange.ChangeId,
                RecordId = pendingChange.RecordId,
                OperationType = pendingChange.OperationType,
                LastModified = pendingChange.LastModified,
                DeviceUserId = pendingChange.DeviceUserId,
                Item = item
            };

            var response = await httpClient.PostAsJsonAsync(
                "pushChanges",
                request);

            response.EnsureSuccessStatusCode();

            await database.MarkChangeSyncedAsync(
                pendingChange.Id);

            pushed++;
        }

        return pushed;
    }

    public async Task<int> PullChangesAsync()
    {
        var lastSync = GetLastSyncTime();
        var pullStartedAt = DateTime.UtcNow;

        var since = Uri.EscapeDataString(
            lastSync.ToString("O"));

        var response = await httpClient.GetAsync(
            $"changes?since={since}");

        response.EnsureSuccessStatusCode();

        var changes = await response.Content
            .ReadFromJsonAsync<List<LostAndFoundItem>>();

        changes ??= new List<LostAndFoundItem>();

        await database.StageDownloadedChangesAsync(changes);

        Preferences.Default.Set(
            PendingPullTimeKey,
            pullStartedAt.ToString("O"));

        return changes.Count;
    }

    public async Task<int> ProcessDownloadedChangesAsync()
    {
        var processed =
            await database.ProcessDownloadedChangesAsync();

        var pendingPullTime = Preferences.Default.Get(
            PendingPullTimeKey,
            string.Empty);

        if (!string.IsNullOrWhiteSpace(pendingPullTime))
        {
            Preferences.Default.Set(
                LastSyncKey,
                pendingPullTime);
        }

        Preferences.Default.Remove(PendingPullTimeKey);

        return processed;
    }
}