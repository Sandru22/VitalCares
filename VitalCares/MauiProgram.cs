using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Syncfusion.Maui.Core.Hosting;

namespace VitalCares;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .ConfigureSyncfusionCore()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});
        builder.Services.AddSingleton<ViewModels.MainViewModel>();
        builder.Services.AddSingleton<ViewModels.HistoryViewModel>();
        builder.Services.AddSingleton<Views.RecommendationsPage>();
        builder.Services.AddSingleton<Views.DashboardPage>();
        builder.Services.AddSingleton<Views.ConnectionTest>();
        builder.Services.AddSingleton<Views.Calendar>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
