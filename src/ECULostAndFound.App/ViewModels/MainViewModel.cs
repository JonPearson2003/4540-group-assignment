using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using ECULostAndFound.App.Models;
using ECULostAndFound.App.Data;


namespace ECULostAndFound.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "ECU Lost and Found";

    [ObservableProperty]
    private string statusMessage = "Welcome to the ECU Lost and Found App!";
    
}