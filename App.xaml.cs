using Microsoft.Extensions.DependencyInjection;

namespace MyMauiApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
      var window = new Window(new AppShell());

		// If there's no user selected, show the login modal when the main window is created.
		if (!SessaoUsuario.ObterId().HasValue)
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				// Delay a bit so Shell has finished initialization
				await Task.Delay(200);
				if (window.Page is Shell shell)
				{
					await shell.Navigation.PushModalAsync(new NavigationPage(new LoginPage()));
				}
			});
		}

		return window;
	}
}