using ECULostAndFound.App.ViewModels;

namespace ECULostAndFound.App;

public partial class BrowsePage : ContentPage
{
    private readonly BrowseViewModel _viewModel;

    public BrowsePage(BrowseViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadItemsAsync();
    }
}
