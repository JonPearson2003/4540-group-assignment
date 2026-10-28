using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ECULostAndFound.App.Models;
using ECULostAndFound.App.Data;

namespace ECULostAndFound.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string DeviceUserId = "Development-device-1";
    private readonly LostAndFoundDatabase _database;
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
    private string _statusMessage = "Welcome to the ECU Lost and Found App!";

    public MainViewModel(LostAndFoundDatabase database)
    {
        _database = database;
    }

    [RelayCommand]
    private async Task LoadItemsAsync()
    {
        var items = await _database.GetLostAndFoundItemsAsync();
        Items = new ObservableCollection<LostAndFoundItem>(items);

        await LoadPendingChangesAsync();
    }

    private async Task LoadPendingChangesAsync()
    {
        var pendingChanges = await _database.GetPendingChangesAsync();
        PendingChanges = new ObservableCollection<PendingSyncChange>(pendingChanges);
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

            var result = await _database.SaveLostAndFoundItemAsync(item);
            if (result <= 0)
            {
                StatusMessage = "Could not save the item.";
                return;
            }

            Items.Add(item);

            var auditChange = new ChangesTable
            {
                Item_Id = item.Id,
                ChangeType = "Create",
                ColumnName = "All",
                OldValue = string.Empty,
                NewValue = $"Title: {item.Title}, Description: {item.Description}, Location: {item.Location}",
                ChangeDate = item.LastUpdated
            };

            await SaveAuditChangeAsync(auditChange);
            await QueueSyncChangeAsync(item.Id, "create", item.LastUpdated);

            StatusMessage = "Item added and queued for sync.";
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

            var result = await _database.SaveLostAndFoundItemAsync(item);
            if (result <= 0)
            {
                StatusMessage = "Could not find item to update.";
                return;
            }

            if (hasChanged)
            {
                var auditChange = new ChangesTable
                {
                    Item_Id = item.Id,
                    ChangeType = "Edit",
                    ColumnName = "All",
                    OldValue = $"Title: {_oldTitle}, Description: {_oldDescription}, Location: {_oldLocation}",
                    NewValue = $"Title: {newTitle}, Description: {newDescription}, Location: {newLocation}",
                    ChangeDate = item.LastUpdated
                };

                await SaveAuditChangeAsync(auditChange);
                await QueueSyncChangeAsync(item.Id, "update", item.LastUpdated);
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
    private async Task DeleteItemAsync(LostAndFoundItem item)
    {
        int deletedItemId = item.Id;
        DateTime deletedAtUtc = DateTime.UtcNow;

        var result = await _database.DeleteLostAndFoundItemAsync(item);
        if (result <= 0)
        {
            StatusMessage = "Could not find item to delete.";
            return;
        }

        var auditChange = new ChangesTable
        {
            Item_Id = deletedItemId,
            ChangeType = "Delete",
            ColumnName = "All",
            OldValue = $"Title: {item.Title}, Description: {item.Description}, Location: {item.Location}",
            NewValue = string.Empty,
            ChangeDate = deletedAtUtc
        };

        await SaveAuditChangeAsync(auditChange);
        await QueueSyncChangeAsync(deletedItemId, "delete", deletedAtUtc);

        Items.Remove(item);
        if (ReferenceEquals(_itemBeingEdited, item))
        {
            _itemBeingEdited = null;
            ClearForm();
        }

        StatusMessage = "Item deleted and queued for sync.";
    }

    private async Task SaveAuditChangeAsync(ChangesTable change)
    {
        int result = await _database.SaveChangeAsync(change);
        if (result <= 0)
        {
            throw new InvalidOperationException(
                $"Could not save the {change.ChangeType} audit record for item {change.Item_Id}.");
        }

        Changes.Add(change);
    }

    private async Task QueueSyncChangeAsync(
        int recordId,
        string operationType,
        DateTime lastModifiedUtc)
    {
        var pendingChange = new PendingSyncChange
        {
            ChangeId = Guid.NewGuid().ToString(),
            RecordId = recordId,
            OperationType = operationType,
            LastModified = lastModifiedUtc,
            DeviceUserId = DeviceUserId,
            IsSynced = false
        };

        int result = await _database.QueuePendingChangeAsync(pendingChange);
        if (result <= 0)
        {
            throw new InvalidOperationException(
                $"Could not queue the {operationType} operation for item {recordId}.");
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
