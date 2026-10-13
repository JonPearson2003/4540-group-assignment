using CommunityToolkit.Mvvm.ComponentModel;

namespace ECULostAndFound.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "ECU Lost and Found";

    [ObservableProperty]
    private string statusMessage = "Welcome to the ECU Lost and Found App!";
    
}