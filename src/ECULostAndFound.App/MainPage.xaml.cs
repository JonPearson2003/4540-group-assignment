using ECULostAndFound.App.ViewModels;

namespace ECULostAndFound.App;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _isListeningForConnectivity;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadItemsCommand.ExecuteAsync(null);

        if (!_isListeningForConnectivity)
        {
            Connectivity.Current.ConnectivityChanged +=
                OnConnectivityChanged;
            _isListeningForConnectivity = true;
        }

        await _viewModel.SyncAutomaticallyAsync();
    }

    protected override void OnDisappearing()
    {
        if (_isListeningForConnectivity)
        {
            Connectivity.Current.ConnectivityChanged -=
                OnConnectivityChanged;
            _isListeningForConnectivity = false;
        }

        base.OnDisappearing();
    }

    private void OnConnectivityChanged(
        object? sender,
        ConnectivityChangedEventArgs args)
    {
        MainThread.BeginInvokeOnMainThread(
            async () => await _viewModel.SyncAutomaticallyAsync());
    }
}