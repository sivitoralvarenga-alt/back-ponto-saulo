using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PontoSaulo.Data;

namespace PontoSaulo;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(_ => { });

		builder.Services.AddSingleton<DatabaseService>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();
		app.Services.GetRequiredService<DatabaseService>().Initialize();
		return app;
	}
}
