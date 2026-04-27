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
				await Task.Delay(200);
				var shell = Application.Current?.MainPage as Shell ?? window.Page as Shell;
				if (shell != null)
					await shell.Navigation.PushModalAsync(new NavigationPage(new LoginPage()));
			});
		}

		return window;
	}
}