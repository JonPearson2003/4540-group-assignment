using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ECULostAndFound.App.Models;
using ECULostAndFound.App.Services;

namespace ECULostAndFound.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly LocalDatabaseService _database;
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

    public MainViewModel(LocalDatabaseService database)
    {
        _database = database;
        LoadItems();
    }

    private void LoadItems()
    {
        Items = new ObservableCollection<LostAndFoundItem>(
            _database.GetItems());
    }

    [RelayCommand]
    private void SaveItem()
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

            _database.CreateItem(item);
            Items.Add(item);
            StatusMessage = "Item added.";
        }
        else
        {
            _itemBeingEdited.Title = ItemTitle.Trim();
            _itemBeingEdited.Description = Description.Trim();
            _itemBeingEdited.Location = Location.Trim();

            if (!_database.UpdateItem(_itemBeingEdited))
            {
                StatusMessage = "Could not find item to update.";
                return;
            }

            LoadItems();
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
    private void DeleteItem(LostAndFoundItem item)
    {
        if (!_database.DeleteItem(item.Id))
        {
            StatusMessage = "Could not find item to delete.";
            return;
        }

        Items.Remove(item);
        StatusMessage = "Item deleted.";
    }

    private void ClearForm()
    {
        ItemTitle = string.Empty;
        Description = string.Empty;
        Location = string.Empty;
    }
}
