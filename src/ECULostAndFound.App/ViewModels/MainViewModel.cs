using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ECULostAndFound.App.Data;
using ECULostAndFound.App.Models;
using ECULostAndFound.App.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
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
    private string _oldCategory = string.Empty;
    private string _oldLocation = string.Empty;
    private DateTime _oldDateFound;
    private string _oldCollectionLocation = string.Empty;
    private string _oldCollectionDetails = string.Empty;
    private string _oldPhotoData = string.Empty;

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
    private string _itemCategory = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private DateTime _dateFound = DateTime.Today;

    [ObservableProperty]
    private string _collectionLocation = string.Empty;

    [ObservableProperty]
    private string _collectionDetails = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    [NotifyPropertyChangedFor(nameof(PhotoActionText))]
    [NotifyPropertyChangedFor(nameof(PhotoPreview))]
    private string _photoData = string.Empty;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemActionButtonText))]
    private bool _isEditingItem;

    public string ItemActionButtonText =>
        IsEditingItem ? "Save changes" : "Publish found item";

    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoData);

    public string PhotoActionText =>
        HasPhoto ? "Replace photo" : "Add photo";

    public ImageSource? PhotoPreview =>
        HasPhoto
            ? ImageSource.FromStream(
                () => new MemoryStream(Convert.FromBase64String(PhotoData)))
            : null;

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
        string newCollectionLocation = CollectionLocation.Trim();
        string newCollectionDetails = CollectionDetails.Trim();

        if (_itemBeingEdited is null)
        {
            if (!await CreateItemAsync("Found"))
            {
                return;
            }
        }
        else
        {
            var item = _itemBeingEdited;

            bool hasChanged =
                _oldTitle != newTitle ||
                _oldDescription != newDescription ||
                _oldCategory != ItemCategory.Trim() ||
                _oldLocation != newLocation ||
                _oldDateFound != DateFound.Date ||
                _oldCollectionLocation != newCollectionLocation ||
                _oldCollectionDetails != newCollectionDetails ||
                _oldPhotoData != PhotoData;

            item.Title = newTitle;
            item.Description = newDescription;
            item.Category = ItemCategory.Trim();
            item.Location = newLocation;
            item.DateFound = DateFound.Date;
            item.CollectionLocation = newCollectionLocation;
            item.CollectionDetails = newCollectionDetails;
            item.PhotoData = PhotoData;

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
                    OldValue = BuildAuditSummary(
                        _oldTitle,
                        _oldDescription,
                        _oldCategory,
                        _oldLocation,
                        _oldDateFound,
                        _oldCollectionLocation,
                        _oldCollectionDetails,
                        !string.IsNullOrWhiteSpace(_oldPhotoData)),
                    NewValue = BuildAuditSummary(
                        newTitle,
                        newDescription,
                        item.Category,
                        newLocation,
                        item.DateFound,
                        item.CollectionLocation,
                        item.CollectionDetails,
                        item.HasPhoto),
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
            IsEditingItem = false;

            StatusMessage = hasChanged
                ? "Item updated and queued for sync."
                : "Item updated. No changes detected.";
        }

        ClearForm();
    }

    private async Task<bool> CreateItemAsync(string reportType)
    {
        var item = new LostAndFoundItem
        {
            Title = ItemTitle.Trim(),
            Description = Description.Trim(),
            Category = ItemCategory.Trim(),
            Location = Location.Trim(),
            DateFound = DateFound.Date,
            CollectionLocation = CollectionLocation.Trim(),
            CollectionDetails = CollectionDetails.Trim(),
            PhotoData = PhotoData,
            ReportType = reportType
        };

        var result =
            await _database.SaveLostAndFoundItemAsync(item);

        if (result <= 0)
        {
            StatusMessage = "Could not save the item.";
            return false;
        }

        Items.Add(item);

        var auditChange = new ChangesTable
        {
            Item_Id = item.Id,
            ChangeType = "Create",
            ColumnName = "All",
            OldValue = string.Empty,
            NewValue = BuildAuditSummary(
                item.Title,
                item.Description,
                item.Category,
                item.Location,
                item.DateFound,
                item.CollectionLocation,
                item.CollectionDetails,
                item.HasPhoto),
            ChangeDate = item.LastUpdated
        };

        await SaveAuditChangeAsync(auditChange);
        await QueueSyncChangeAsync(
            item,
            "create",
            item.LastUpdated);

        StatusMessage = "Item added and queued for sync.";
        return true;
    }

    [RelayCommand]
    private void EditItem(LostAndFoundItem item)
    {
        _itemBeingEdited = item;
        IsEditingItem = true;

        _oldTitle = item.Title;
        _oldDescription = item.Description;
        _oldCategory = item.Category;
        _oldLocation = item.Location;
        _oldDateFound = item.DateFound.Date;
        _oldCollectionLocation = item.CollectionLocation;
        _oldCollectionDetails = item.CollectionDetails;
        _oldPhotoData = item.PhotoData;

        ItemTitle = item.Title;
        Description = item.Description;
        ItemCategory = item.Category;
        Location = item.Location;
        DateFound = item.DateFound.Date;
        CollectionLocation = item.CollectionLocation;
        CollectionDetails = item.CollectionDetails;
        PhotoData = item.PhotoData;

        StatusMessage = "Editing item.";
    }

    [RelayCommand]
    private async Task PickPhotoAsync()
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync(
                new MediaPickerOptions
                {
                    Title = "Choose item photo",
                    SelectionLimit = 1
                });

            if (photos.Count == 0)
            {
                return;
            }

            var photo = photos[0];
            await using var photoStream = await photo.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            const int maxPhotoSizeBytes = 8 * 1024 * 1024;
            var buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await photoStream.ReadAsync(buffer)) > 0)
            {
                if (memoryStream.Length + bytesRead > maxPhotoSizeBytes)
                {
                    StatusMessage = "Choose a photo smaller than 8 MB.";
                    return;
                }

                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            PhotoData = Convert.ToBase64String(memoryStream.ToArray());
            StatusMessage = "Photo added. It will be included in the next sync.";
        }
        catch (FeatureNotSupportedException)
        {
            StatusMessage = "Photo selection is not supported on this device.";
        }
        catch (PermissionException)
        {
            StatusMessage = "Photo access was denied. Allow photo access and try again.";
        }
        catch (IOException exception)
        {
            StatusMessage = $"Could not read the selected photo: {exception.Message}";
        }
    }

    [RelayCommand]
    private void RemovePhoto()
    {
        PhotoData = string.Empty;
        StatusMessage = "Photo removed. Save the item to sync this change.";
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
            OldValue = BuildAuditSummary(
                item.Title,
                item.Description,
                item.Category,
                item.Location,
                item.DateFound,
                item.CollectionLocation,
                item.CollectionDetails,
                item.HasPhoto),
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
        IsEditingItem = false;
        ItemTitle = string.Empty;
        Description = string.Empty;
        ItemCategory = string.Empty;
        Location = string.Empty;
        DateFound = DateTime.Today;
        CollectionLocation = string.Empty;
        CollectionDetails = string.Empty;
        PhotoData = string.Empty;
    }

    private static string BuildAuditSummary(
        string title,
        string description,
        string category,
        string location,
        DateTime dateFound,
        string collectionLocation,
        string collectionDetails,
        bool hasPhoto) =>
        $"Title: {title}, Description: {description}, Category: {category}, Where found: {location}, Date found: {dateFound:MMM d, yyyy}, Collection location: {collectionLocation}, Collection details: {collectionDetails}, Photo attached: {hasPhoto}";
}