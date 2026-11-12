using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ECULostAndFound.App.Data;
using ECULostAndFound.App.Models;
using ECULostAndFound.App.Services;
using Microsoft.Maui.Networking;
using System.Text.Json;

namespace ECULostAndFound.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string DeviceUserId = "Development-device-1";

    private readonly LostAndFoundDatabase _database;
    private readonly SyncService _syncService;
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    private LostAndFoundItem? _itemBeingEdited;

    private string _oldTitle = string.Empty;
    private string _oldDescription = string.Empty;
    private string _oldLocation = string.Empty;

    [ObservableProperty]
    private string _pageTitle = "ECU Lost and Found";

    [ObservableProperty]
    private ObservableCollection<LostAndFoundItem> _items = new();

    [ObservableProperty]
    private ObservableCollection<ChangesTable> _changes = new();

    [ObservableProperty]
    private ObservableCollection<PendingSyncChange> _pendingChanges = new();

    [ObservableProperty]
    private string _itemTitle = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _statusMessage =
        "Welcome to the ECU Lost and Found App!";

    [ObservableProperty]
    private bool _isSyncing;

    [ObservableProperty]
    private bool _isItemsView = true;

    [ObservableProperty]
    private bool _isPendingChangesView;

    [ObservableProperty]
    private bool _isAuditView;

    public MainViewModel(
        LostAndFoundDatabase database,
        SyncService syncService)
    {
        _database = database;
        _syncService = syncService;
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        var items =
            await _database.GetLostAndFoundItemsAsync();

        Items =
            new ObservableCollection<LostAndFoundItem>(
                items);

        await LoadPendingChangesAsync();
        await LoadAuditEntriesAsync();
    }

    private async Task LoadPendingChangesAsync()
    {
        var pendingChanges =
            await _database.GetPendingChangesAsync();

        PendingChanges =
            new ObservableCollection<PendingSyncChange>(
                pendingChanges);
    }

    private async Task LoadAuditEntriesAsync()
    {
        var changes = await _database.GetChangesAsync();

        Changes = new ObservableCollection<ChangesTable>(changes);
    }

    [RelayCommand]
    private void ShowItems() => SetListView(isItems: true);

    [RelayCommand]
    private void ShowPendingChanges() => SetListView(isPendingChanges: true);

    [RelayCommand]
    private void ShowAuditTrail() => SetListView(isAudit: true);

    private void SetListView(
        bool isItems = false,
        bool isPendingChanges = false,
        bool isAudit = false)
    {
        IsItemsView = isItems;
        IsPendingChangesView = isPendingChanges;
        IsAuditView = isAudit;
    }

    [RelayCommand]
    private Task SyncNowAsync() => RunSyncAsync(isAutomatic: false);

    public Task SyncAutomaticallyAsync() =>
        RunSyncAsync(isAutomatic: true);

    private async Task RunSyncAsync(bool isAutomatic)
    {
        if (!await _syncGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                StatusMessage = isAutomatic
                    ? "Automatic sync is waiting for an internet connection. Changes remain queued."
                    : "You're offline. Pending changes remain queued until you sync online.";
                return;
            }

            IsSyncing = true;
            StatusMessage = isAutomatic
                ? "Connection detected. Syncing..."
                : "Syncing with server...";

            var pushResult =
                await _syncService.PushChangesAsync();

            var pulled =
                await _syncService.PullChangesAsync();

            await LoadItemsAsync();

            StatusMessage =
                $"Sync complete. Uploaded {pushResult.ItemChanges} item change(s) and {pushResult.AuditEntries} audit entry(ies); applied {pulled} server change(s).";
        }
        catch (HttpRequestException)
        {
            StatusMessage =
                "Could not reach the server. Changes that were not uploaded remain queued.";
        }
        catch (TaskCanceledException)
        {
            StatusMessage =
                "Sync timed out. Changes that were not uploaded remain queued.";
        }
        catch (Exception exception)
        {
            StatusMessage =
                $"Sync failed: {exception.Message}";
        }
        finally
        {
            IsSyncing = false;
            _syncGate.Release();
        }
    }

    [RelayCommand]
    private async Task SaveItemAsync()
    {
        if (string.IsNullOrWhiteSpace(ItemTitle))
        {
            StatusMessage = "Enter an item name.";
            return;
        }

        string newTitle = ItemTitle.Trim();
        string newDescription = Description.Trim();
        string newLocation = Location.Trim();

        if (_itemBeingEdited is null)
        {
            var item = new LostAndFoundItem
            {
                Title = newTitle,
                Description = newDescription,
                Location = newLocation
            };

            var result =
                await _database
                    .SaveLostAndFoundItemAsync(item);

            if (result <= 0)
            {
                StatusMessage =
                    "Could not save the item.";

                return;
            }

            Items.Add(item);

            var auditChange = new ChangesTable
            {
                Item_Id = item.Id,
                ChangeType = "Create",
                ColumnName = "All",
                OldValue = string.Empty,
                NewValue =
                    $"Title: {item.Title}, Description: {item.Description}, Location: {item.Location}",
                ChangeDate = item.LastUpdated
            };

            await SaveAuditChangeAsync(auditChange);

            await QueueSyncChangeAsync(
                item,
                "create",
                item.LastUpdated);

            StatusMessage =
                "Item added and queued for sync.";
        }
        else
        {
            var item = _itemBeingEdited;

            bool hasChanged =
                _oldTitle != newTitle ||
                _oldDescription != newDescription ||
                _oldLocation != newLocation;

            item.Title = newTitle;
            item.Description = newDescription;
            item.Location = newLocation;

            var result =
                await _database
                    .SaveLostAndFoundItemAsync(item);

            if (result <= 0)
            {
                StatusMessage =
                    "Could not find item to update.";

                return;
            }

            if (hasChanged)
            {
                var auditChange = new ChangesTable
                {
                    Item_Id = item.Id,
                    ChangeType = "Edit",
                    ColumnName = "All",
                    OldValue =
                        $"Title: {_oldTitle}, Description: {_oldDescription}, Location: {_oldLocation}",
                    NewValue =
                        $"Title: {newTitle}, Description: {newDescription}, Location: {newLocation}",
                    ChangeDate = item.LastUpdated
                };

                await SaveAuditChangeAsync(
                    auditChange);

                await QueueSyncChangeAsync(
                    item,
                    "update",
                    item.LastUpdated);
            }

            await LoadItemsAsync();

            _itemBeingEdited = null;

            StatusMessage = hasChanged
                ? "Item updated and queued for sync."
                : "Item updated. No changes detected.";
        }

        ClearForm();
    }

    [RelayCommand]
    private void EditItem(LostAndFoundItem item)
    {
        _itemBeingEdited = item;

        _oldTitle = item.Title;
        _oldDescription = item.Description;
        _oldLocation = item.Location;

        ItemTitle = item.Title;
        Description = item.Description;
        Location = item.Location;

        StatusMessage = "Editing item.";
    }

    [RelayCommand]
    private async Task DeleteItemAsync(
        LostAndFoundItem item)
    {
        int deletedItemId = item.Id;

        var result =
            await _database
                .DeleteLostAndFoundItemAsync(item);

        if (result <= 0)
        {
            StatusMessage =
                "Could not find item to delete.";

            return;
        }

        DateTime deletedAtUtc = item.LastUpdated;

        var auditChange = new ChangesTable
        {
            Item_Id = deletedItemId,
            ChangeType = "Delete",
            ColumnName = "All",
            OldValue =
                $"Title: {item.Title}, Description: {item.Description}, Location: {item.Location}",
            NewValue = string.Empty,
            ChangeDate = deletedAtUtc
        };

        await SaveAuditChangeAsync(auditChange);

        await QueueSyncChangeAsync(
            item,
            "delete",
            deletedAtUtc);

        Items.Remove(item);

        if (ReferenceEquals(
            _itemBeingEdited,
            item))
        {
            _itemBeingEdited = null;
            ClearForm();
        }

        StatusMessage =
            "Item deleted and queued for sync.";
    }

    private async Task SaveAuditChangeAsync(
        ChangesTable change)
    {
        change.ActorId = DeviceUserId;
        change.Source = "Mobile app";

        int result =
            await _database.SaveChangeAsync(change);

        if (result <= 0)
        {
            throw new InvalidOperationException(
                $"Could not save the {change.ChangeType} audit record for item {change.Item_Id}.");
        }

        await LoadAuditEntriesAsync();
    }

    private async Task QueueSyncChangeAsync(
        LostAndFoundItem item,
        string operationType,
        DateTime lastModifiedUtc)
    {
        var pendingChange = new PendingSyncChange
        {
            ChangeId = Guid.NewGuid().ToString(),
            RecordId = item.Id,
            OperationType = operationType,
            LastModified = lastModifiedUtc,
            DeviceUserId = DeviceUserId,
            ItemJson = operationType.Equals(
                "delete",
                StringComparison.OrdinalIgnoreCase)
                    ? null
                    : JsonSerializer.Serialize(item)
        };

        int result = await _database.QueuePendingChangeAsync(pendingChange);

        if (result <= 0)
        {
            throw new InvalidOperationException(
                $"Could not queue the {operationType} operation for item {item.Id}.");
        }

        await LoadPendingChangesAsync();
    }

    private void ClearForm()
    {
        ItemTitle = string.Empty;
        Description = string.Empty;
        Location = string.Empty;
    }
}