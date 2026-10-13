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
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

			// Services
			builder.Services.AddSingleton<LocalDatabaseService>();

			// ViewModels
			builder.Services.AddTransient<MainViewModel>();

			//Pages
			builder.Services.AddTransient<MainPage>();
	
	#if DEBUG
		builder.Logging.AddDebug();
	#endif

		return builder.Build();
	}
}