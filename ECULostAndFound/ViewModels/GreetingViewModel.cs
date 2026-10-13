using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MvvmDemoApp.ViewModels;

public partial class GreetingViewModel : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string greetingText = "Hello!";

    [RelayCommand]
    private void UpdateGreeting()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            GreetingText = "Please enter your name.";
            return;
        }

        GreetingText = $"Welcome, {Name}!";
    }
}