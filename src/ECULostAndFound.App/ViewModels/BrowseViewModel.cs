using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ECULostAndFound.App.Data;
using ECULostAndFound.App.Models;
using Microsoft.Extensions.Logging;

namespace ECULostAndFound.App.ViewModels;

public partial class BrowseViewModel : ObservableObject
{
    private const string AllItemsCategory = "All items";

    private readonly LostAndFoundDatabase _database;
    private readonly ILogger<BrowseViewModel> _logger;
    private List<LostAndFoundItem> _allItems = [];

    [ObservableProperty]
    public partial ObservableCollection<LostAndFoundItem> FilteredItems { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<string> Categories { get; set; } =
        new([AllItemsCategory]);

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedCategory { get; set; } = AllItemsCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadError))]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial string LoadError { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    public partial bool IsLoading { get; set; }

    public bool IsNotLoading => !IsLoading && !HasLoadError;

    public bool HasLoadError => !string.IsNullOrEmpty(LoadError);

    public BrowseViewModel(
        LostAndFoundDatabase database,
        ILogger<BrowseViewModel> logger)
    {
        _database = database;
        _logger = logger;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

    public async Task LoadItemsAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            LoadError = string.Empty;

            _allItems = await _database.GetLostAndFoundItemsAsync();
            UpdateCategories();
            ApplyFilters();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not load items for the browse page.");
            LoadError = "Items could not be loaded. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCategories()
    {
        var categories = _allItems
            .Select(item => item.Category?.Trim())
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase)
            .Cast<string>()
            .ToList();

        Categories = new ObservableCollection<string>(
            [AllItemsCategory, .. categories]);

        if (!Categories.Contains(
                SelectedCategory,
                StringComparer.OrdinalIgnoreCase))
        {
            SelectedCategory = AllItemsCategory;
        }
    }

    private void ApplyFilters()
    {
        string searchText = SearchText.Trim();
        string selectedCategory = SelectedCategory;

        var results = _allItems
            .Where(item =>
                (selectedCategory == AllItemsCategory ||
                 string.Equals(
                     item.Category,
                     selectedCategory,
                     StringComparison.OrdinalIgnoreCase)) &&
                (searchText.Length == 0 ||
                 Matches(item.Title, searchText) ||
                 Matches(item.Description, searchText) ||
                 Matches(item.Category, searchText) ||
                 Matches(item.Location, searchText) ||
                 Matches(item.CollectionLocation, searchText) ||
                 Matches(item.CollectionDetails, searchText) ||
                 Matches(item.ReportType, searchText)))
            .OrderByDescending(item => item.DateReported)
            .ToList();

        FilteredItems = new ObservableCollection<LostAndFoundItem>(results);
    }

    private static bool Matches(string? value, string searchText) =>
        value?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
}
