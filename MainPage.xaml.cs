using System.Globalization;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;
using PontoSaulo.Data;
using PontoSaulo.Models;


namespace MyMauiApp;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		BindingContext = new RelogioViewModel();
		Geolocalizar();

		var cultura = new CultureInfo("pt-BR");
   		CultureInfo.DefaultThreadCurrentCulture = cultura;
    	CultureInfo.DefaultThreadCurrentUICulture = cultura;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		AtualizarRotuloUsuario();

	}

	private void AtualizarRotuloUsuario()
	{
		var id = SessaoUsuario.ObterId();
		if (!id.HasValue)
		{
			LabelUsuarioAtual.Text = "Nenhum usuário selecionado — use a aba Usuários.";
			ImagemUsuario.Source = null;
			ImagemUsuario.IsVisible = false;
			return;
		}

		var db = new DatabaseService();
		var u = db.ObterUsuario(id.Value);
		if (u == null)
		{
			LabelUsuarioAtual.Text = "Usuário salvo não encontrado. Escolha outro na aba Usuários.";
			ImagemUsuario.Source = null;
			ImagemUsuario.IsVisible = false;
			return;
		}

		LabelUsuarioAtual.Text = $"Registrando como: {u.Nome}";
		var img = ImagemDeBase64(u.Facedata);
		ImagemUsuario.Source = img;
		ImagemUsuario.IsVisible = img != null;
	}

	private static ImageSource? ImagemDeBase64(string? base64)
	{
		if (string.IsNullOrWhiteSpace(base64))
			return null;
		try
		{
			var bytes = Convert.FromBase64String(base64.Trim());
			return ImageSource.FromStream(() => new MemoryStream(bytes));
		}
		catch (FormatException)
		{
			return null;
		}
	}


	private async void OnBotaoEntradaClicked(object? sender, EventArgs e)
	{
		var tela = Application.Current?.Windows[0]?.Page;
		var _clicado = false;

		if (BotaoEntrada.Background is SolidColorBrush brush && brush.Color == Colors.DimGray)
		{
			_clicado = true;
		}

		if (tela != null && !_clicado)
		{
			bool resposta = await tela.DisplayAlertAsync(
				"Confirmação",
				"Deseja marcar uma entrada de ponto?",
				"Sim",
				"Não"
			);

			if (resposta)
			{
				BotaoEntrada.Background = Colors.DimGray;
				await RegistrarPonto("Entrada");
				SemanticScreenReader.Announce(BotaoEntrada.Text);
			}
		}

	}

	private static async Task<bool> EnsureLocationPermissionAsync()
	{
		var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
		if (status == PermissionStatus.Granted)
			return true;

		if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
		{
			// On iOS once denied you must prompt the user to go to settings
			if (Application.Current?.MainPage is Page pagina)
				await pagina.DisplayAlert("Permissão", "Permissão de localização negada. Habilite em Ajustes.", "OK");
			return false;
		}

		status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
		return status == PermissionStatus.Granted;
       }

	private async void OnBotaoSaidaClicked(object? sender, EventArgs e)
	{
		var tela = Application.Current?.Windows[0]?.Page;
		var _clicado = false;

		if(BotaoSaida.Background is SolidColorBrush brush && brush.Color == Colors.DimGray)
		{
			_clicado = true;
		}
		
		if(tela != null && !_clicado)
		{
			bool resposta = await tela.DisplayAlertAsync(
				"Confirmação",
				"Deseja marcar uma saída de ponto?",
				"Sim",
				"Não"
			);

			if (resposta)
			{
				BotaoSaida.Background= Colors.DimGray;
				await RegistrarPonto("Saida");
				SemanticScreenReader.Announce(BotaoSaida.Text);
			}
		}
	}

	private async void Geolocalizar()
	{
		try
		{
			if (!await EnsureLocationPermissionAsync())
				return;

			var local = await Geolocation.GetLocationAsync(
				new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10))
			);

			if (local == null)
			{
				local = await Geolocation.GetLocationAsync(
					new GeolocationRequest(GeolocationAccuracy.High)
				);
			}

			if (local == null)
				return;

			var posicao = SphericalMercator.FromLonLat(
				local.Longitude,
				local.Latitude
			);

			var mapa = new Mapsui.Map();

			mapa.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

			await Task.Delay(500);

			ControleMapa.Map = mapa;

			ControleMapa.Map.Widgets.Clear();
			ControleMapa.Map.Navigator.CenterOn(posicao.x, posicao.y);
			ControleMapa.Map.Navigator.ZoomTo(5);
		}
		catch (FeatureNotSupportedException)
		{
			// Geolocalização indisponível neste dispositivo.
		}
		catch (PermissionException)
		{
			// Permissão negada após fluxo anterior.
		}
		catch (Exception)
		{
			// Timeout de GPS, mapa ou hardware — app continua sem mapa.
		}
	} 

	private async Task RegistrarPonto(string tipo)
	{
		var userId = SessaoUsuario.ObterId();
		if (!userId.HasValue)
		{
			await DisplayAlert(
				"Usuário",
				"Selecione um usuário na aba Usuários antes de registrar o ponto.",
				"OK");
			return;
		}

		var db = new DatabaseService();

     Location? local = null;
		if (await EnsureLocationPermissionAsync())
		{
			try
			{
				local = await Geolocation.GetLocationAsync(
					new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10))
				);
			}
			catch (PermissionException)
			{
				// Permission was denied after request - continue without location
				local = null;
			}
		}

		var agoraUtc = DateTime.UtcNow;

		var registro = new TimeRecord
		{
			UserId = userId.Value,
			Timestamp = agoraUtc.ToString("o"),
			Latitude = local?.Latitude,
			Longitude = local?.Longitude,
			Type = tipo,
			AuthMethod = "manual",
			Sucess = true
		};

		db.InsertPonto(registro);

		var usuario = db.ObterUsuario(userId.Value);
		var telefone = usuario?.Telefone?.Trim();
		if (!string.IsNullOrEmpty(telefone) && Sms.Default.IsComposeSupported)
		{
			var quando = DateTime.Now;
			var texto = $"Confirmacao de ponto batido em {quando:dd/MM/yyyy} {quando:HH:mm}";
			try
			{
				await Sms.Default.ComposeAsync(new SmsMessage(texto, telefone));
			}
			catch
			{
				// Falha ao abrir app de SMS ou envio cancelado — ponto já foi salvo.
			}
		}
	}

	private async void OnLogoutClicked(object? sender, EventArgs e)
	{
		SessaoUsuario.Limpar();
		AtualizarRotuloUsuario();
		// Show login modal
		if (Application.Current?.MainPage is Shell shell)
		{
			await shell.Navigation.PushModalAsync(new NavigationPage(new LoginPage()));
		}
	}

	private async void OnMeusRegistrosClicked(object? sender, EventArgs e)
	{
		if (Application.Current?.MainPage is Shell shell)
		{
			await shell.Navigation.PushAsync(new MeusRegistrosPage());
		}
	}
}
