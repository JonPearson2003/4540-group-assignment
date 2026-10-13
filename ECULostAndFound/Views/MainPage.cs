using MvvmDemoApp.Views;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new GreetingViewModel();
    }
}