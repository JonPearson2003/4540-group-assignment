using ECULostAndFound.App.Data;
using ECULostAndFound.App.Services;
using ECULostAndFound.App.ViewModels;
using Microsoft.Extensions.Logging;

namespace ECULostAndFound.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont(
                    "OpenSans-Regular.ttf",
                    "OpenSansRegular");

                fonts.AddFont(
                    "OpenSans-Semibold.ttf",
                    "OpenSansSemibold");
            });

        builder.Services.AddSingleton<LostAndFoundDatabase>();

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(ApiSettings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        });

        builder.Services.AddSingleton<SyncService>();

        builder.Services.AddTransient<MainViewModel>();

        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}