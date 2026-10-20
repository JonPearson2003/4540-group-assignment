using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ECULostAndFound.App.Models;
using ECULostAndFound.App.Data;

namespace ECULostAndFound.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
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
    }

    
[RelayCommand]
private async Task SaveItemAsync()
{
    if (string.IsNullOrWhiteSpace(ItemTitle))
    {
        StatusMessage = "Enter an item name.";
        return;
    }

    if (_itemBeingEdited is null)
    {
        var item = new LostAndFoundItem
        {
            Title = ItemTitle.Trim(),
            Description = Description.Trim(),
            Location = Location.Trim()
        };

        var result = await _database.SaveLostAndFoundItemAsync(item);
        if (result <= 0)
        {
            StatusMessage = "Could not save the item.";
            return;
        }

        Items.Add(item);
        StatusMessage = "Item added.";

        var change = new ChangesTable
        {
            Item_Id = item.Id,
            ChangeType = "Create",
            ColumnName = "All",
            OldValue = string.Empty,
            NewValue = $"Title: {item.Title}, Description: {item.Description}, Location: {item.Location}",
            ChangeDate = DateTime.UtcNow
        };

        Changes.Add(change);
        await _database.SaveChangeAsync(change);
    }
    else
    {

        string newTitle = ItemTitle.Trim();
        string newDescription = Description.Trim();
        string newLocation = Location.Trim();

        bool hasChanged =
            _oldTitle != newTitle ||
            _oldDescription != newDescription ||
            _oldLocation != newLocation;

        // Apply the new values
        _itemBeingEdited.Title = newTitle;
        _itemBeingEdited.Description = newDescription;
        _itemBeingEdited.Location = newLocation;

        var result = await _database.SaveLostAndFoundItemAsync(
            _itemBeingEdited);

        if (result <= 0)
        {
            StatusMessage = "Could not find item to update.";
            return;
        }

        // Add a change record only if there was a change. Records new and old values for all fields in a single change record.
        if (hasChanged)
        {
            var change = new ChangesTable
            {
                Item_Id = _itemBeingEdited.Id,
                ChangeType = "Edit",
                ColumnName = "All",
                OldValue = $"Title: {_oldTitle}, Description: {_oldDescription}, Location: {_oldLocation}",
                NewValue = $"Title: {newTitle}, Description: {newDescription}, Location: {newLocation}",
                ChangeDate = DateTime.UtcNow
            };

            Changes.Add(change);
            await _database.SaveChangeAsync(change);
        }

        await LoadItemsAsync();

        _itemBeingEdited = null;
        StatusMessage = hasChanged
            ? "Item updated and change recorded."
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

        // Changes.Add(new ChangesTable
        // {
        //     Item_Id = item.Id,
        //     ChangeType = "Edit",
        //     ColumnName = "All",
        //     OldValue = $"Title: {item.Title}, Description: {item.Description}, Location: {item.Location}",
        //     NewValue = string.Empty,
        //     ChangeDate = DateTime.UtcNow
        // });
        
    }

    [RelayCommand]
    private async Task DeleteItemAsync(LostAndFoundItem item)
    {
        var result = await _database.DeleteLostAndFoundItemAsync(item);
        if (result <= 0)
        {
            StatusMessage = "Could not find item to delete.";
            return;
        }

        Items.Remove(item);
        if (ReferenceEquals(_itemBeingEdited, item))
        {
            _itemBeingEdited = null;
            ClearForm();
        }

        StatusMessage = "Item deleted.";
    }

    private void ClearForm()
    {
        ItemTitle = string.Empty;
        Description = string.Empty;
        Location = string.Empty;
    }
}
