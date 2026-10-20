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

    [ObservableProperty]
    private string _pageTitle = "ECU Lost and Found";

    [ObservableProperty]
    private ObservableCollection<LostAndFoundItem> _items = new();

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
        }
        else
        {
            _itemBeingEdited.Title = ItemTitle.Trim();
            _itemBeingEdited.Description = Description.Trim();
            _itemBeingEdited.Location = Location.Trim();

            var result = await _database.SaveLostAndFoundItemAsync(_itemBeingEdited);
            if (result <= 0)
            {
                StatusMessage = "Could not find item to update.";
                return;
            }

            await LoadItemsAsync();
            _itemBeingEdited = null;
            StatusMessage = "Item updated.";
        }

        ClearForm();
    }

    [RelayCommand]
    private void EditItem(LostAndFoundItem item)
    {
        _itemBeingEdited = item;
        ItemTitle = item.Title;
        Description = item.Description;
        Location = item.Location;
        StatusMessage = "Editing item.";
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
