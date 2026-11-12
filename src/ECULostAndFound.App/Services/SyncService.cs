using System.Text.Json;
using ECULostAndFound.App.Data;
using ECULostAndFound.App.Models;
using Microsoft.Maui.Storage;
using System.Net;

namespace ECULostAndFound.App.Services;

public class SyncService
{
    private const int MaxRequestAttempts = 3;
    private static readonly TimeSpan InitialRetryDelay =
        TimeSpan.FromMilliseconds(500);

    private const string LastServerVersionKey = "LastServerVersion";

    private readonly HttpClient httpClient;
    private readonly LostAndFoundDatabase database;

    public SyncService(
        HttpClient httpClient,
        LostAndFoundDatabase database)
    {
        this.httpClient = httpClient;
        this.database = database;
    }

    public long GetLastServerVersion() =>
        Preferences.Default.Get(LastServerVersionKey, 0L);

    public async Task<(int ItemChanges, int AuditEntries)> PushChangesAsync()
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
                if (string.IsNullOrWhiteSpace(pendingChange.ItemJson))
                {
                    throw new InvalidOperationException(
                        $"Pending change {pendingChange.ChangeId} has no item snapshot.");
                }

                item = JsonSerializer.Deserialize<LostAndFoundItem>(
                    pendingChange.ItemJson)
                    ?? throw new InvalidOperationException(
                        $"Could not read the item snapshot for change {pendingChange.ChangeId}.");
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

            using var response = await SendWithRetryAsync(
                () => httpClient.PostAsJsonAsync(
                    "pushChanges",
                    request));

            var pushResult = await response.Content
                .ReadFromJsonAsync<PushChangeResponse>()
                ?? throw new InvalidOperationException(
                    $"The server returned no result for change {pendingChange.ChangeId}.");

            if (pushResult.ChangeId != pendingChange.ChangeId ||
                pushResult.ServerVersion <= 0)
            {
                throw new InvalidOperationException(
                    $"The server returned an invalid result for change {pendingChange.ChangeId}.");
            }

            await database.SetServerVersionAsync(
                pendingChange.RecordId,
                pushResult.ServerVersion);

            await database.MarkChangeSyncedAsync(
                pendingChange.Id);

            pushed++;
        }

        var auditEntries = await database.GetUnsyncedAuditEntriesAsync();
        int auditEntriesPushed = 0;

        foreach (var auditEntry in auditEntries)
        {
            if (string.IsNullOrWhiteSpace(auditEntry.ChangeId))
            {
                throw new InvalidOperationException(
                    $"Audit entry {auditEntry.Change_Id} has no ChangeId.");
            }

            var request = new AuditEntryRequest
            {
                ChangeId = auditEntry.ChangeId,
                ItemId = auditEntry.Item_Id,
                ChangeType = auditEntry.ChangeType,
                ColumnName = auditEntry.ColumnName,
                OldValue = auditEntry.OldValue,
                NewValue = auditEntry.NewValue,
                ChangedAtUtc = auditEntry.ChangeDate,
                ActorId = auditEntry.ActorId,
                Source = auditEntry.Source
            };

            using var response = await SendWithRetryAsync(
                () => httpClient.PostAsJsonAsync(
                    "audit",
                    request));

            var markedSynced =
                await database.MarkAuditEntrySyncedAsync(
                    auditEntry.Change_Id);

            if (markedSynced <= 0)
            {
                throw new InvalidOperationException(
                    $"Could not mark audit entry {auditEntry.Change_Id} as synced.");
            }

            auditEntriesPushed++;
        }

        return (pushed, auditEntriesPushed);
    }

    public async Task<int> PullChangesAsync()
    {
        var afterVersion = GetLastServerVersion();

        using var response = await SendWithRetryAsync(
            () => httpClient.GetAsync(
                $"changes?afterVersion={afterVersion}"));

        var changes = await response.Content
            .ReadFromJsonAsync<List<ServerChangeDto>>()
            ?? new List<ServerChangeDto>();

        await database.StageDownloadedChangesAsync(changes);

        return await ProcessDownloadedChangesAsync();
    }

    public async Task<int> ProcessDownloadedChangesAsync()
    {
        var stagedChanges =
            await database.GetDownloadedChangesAsync();

        if (stagedChanges.Count == 0)
        {
            return 0;
        }

        long highestVersion = stagedChanges.Max(
            change => change.ServerVersion);

        var processed =
            await database.ProcessDownloadedChangesAsync();

        Preferences.Default.Set(
            LastServerVersionKey,
            Math.Max(GetLastServerVersion(), highestVersion));

        return processed;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> sendRequest)
    {
        var retryDelay = InitialRetryDelay;
        Exception? lastError = null;

        for (int attempt = 1; attempt <= MaxRequestAttempts; attempt++)
        {
            HttpResponseMessage? response = null;
            bool shouldRetry = false;

            try
            {
                response = await sendRequest();
            }
            catch (HttpRequestException exception)
            {
                if (attempt == MaxRequestAttempts)
                {
                    throw;
                }

                lastError = exception;
                shouldRetry = true;
            }
            catch (TaskCanceledException exception)
            {
                if (attempt == MaxRequestAttempts)
                {
                    throw;
                }

                lastError = exception;
                shouldRetry = true;
            }

            if (response is not null)
            {
                if (attempt < MaxRequestAttempts &&
                    IsTransientStatusCode(response.StatusCode))
                {
                    shouldRetry = true;
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                    return response;
                }
            }

            response?.Dispose();

            if (!shouldRetry)
            {
                throw new InvalidOperationException(
                    "The request could not be completed.");
            }

            await Task.Delay(retryDelay);
            retryDelay = TimeSpan.FromTicks(retryDelay.Ticks * 2);
        }

        throw new InvalidOperationException(
            "The request retry loop ended unexpectedly.",
            lastError);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        (int)statusCode == 429 ||
        (int)statusCode >= 500;
}
