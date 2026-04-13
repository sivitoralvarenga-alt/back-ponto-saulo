using System.Globalization;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.Devices.Sensors;
using BruTile.Web;
using BruTile.Predefined;
using Mapsui.Tiling.Layers;
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

		var db = new DatabaseService();
		

		var cultura = new CultureInfo("pt-BR");
   		CultureInfo.DefaultThreadCurrentCulture = cultura;
    	CultureInfo.DefaultThreadCurrentUICulture = cultura;
	}

	private async void OnBotaoEntradaClicked(object? sender, EventArgs e)
	{
		var tela = Application.Current?.Windows[0]?.Page;
		var _clicado = false;

		if(BotaoEntrada.Background is SolidColorBrush brush && brush.Color == Colors.DimGray)
		{
			_clicado = true;
		}
		
		if(tela != null && !_clicado)
		{
			bool resposta = await tela.DisplayAlertAsync(
				"Confirmação",
				"Deseja marcar uma entrada de ponto?",
				"Sim",
				"Não"
			);

			if (resposta)
			{
				BotaoEntrada.Background= Colors.DimGray;
				await RegistrarPonto("Entrada");
				SemanticScreenReader.Announce(BotaoEntrada.Text);
			}
		}
		
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
				"Deseja marcar uma entrada de ponto?",
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
		var local = await Geolocation.GetLocationAsync(
   			new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10))
		);

		if(local == null)
		{
			local = await Geolocation.GetLocationAsync(
				new GeolocationRequest(GeolocationAccuracy.High)
			);
		}

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

	private async Task RegistrarPonto(string tipo)
	{
	var db = new DatabaseService();

    var local = await Geolocation.GetLocationAsync(
        new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10))
    );

    var agoraUtc = DateTime.UtcNow;

    var registro = new TimeRecord
    {
        UserId = 1, // depois você pode trocar por usuário real
        Timestamp = agoraUtc.ToString("o"),
        Latitude = local?.Latitude,
        Longitude = local?.Longitude,
        Type = tipo,
        AuthMethod = "manual",
        Sucess = true
    };

    	db.InsertPonto(registro);
	}
}
