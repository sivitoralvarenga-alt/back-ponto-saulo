using System.Globalization;
using PontoSaulo.Data;
using PontoSaulo.Models;

namespace MyMauiApp;

public sealed class RegistroPontoViewItem
{
	public string Tipo { get; init; } = "";
	public string DataHora { get; init; } = "";
	public string? Localizacao { get; init; }
	public bool TemLocalizacao => !string.IsNullOrEmpty(Localizacao);
}

public partial class MeusRegistrosPage : ContentPage
{
	private static readonly CultureInfo PtBr = new("pt-BR");

	public MeusRegistrosPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		Carregar();
	}

	private async void OnVoltarClicked(object? sender, EventArgs e)
	{
		if (Application.Current?.MainPage is Shell shell)
		{
			await shell.Navigation.PopAsync();
		}
	}

	private void Carregar()
	{
		var id = SessaoUsuario.ObterId();
		if (!id.HasValue)
		{
			LabelContexto.Text = "Selecione um usuário na aba Usuários para ver o histórico.";
			ListaRegistros.ItemsSource = Array.Empty<RegistroPontoViewItem>();
			return;
		}

		var db = new DatabaseService();
		var usuario = db.ObterUsuario(id.Value);
		if (usuario == null)
		{
			LabelContexto.Text = "Usuário não encontrado. Escolha outro na aba Usuários.";
			ListaRegistros.ItemsSource = Array.Empty<RegistroPontoViewItem>();
			return;
		}

		LabelContexto.Text = $"Registros de {usuario.Nome} (mais recentes primeiro).";

		var registros = db.ListarRegistrosDoUsuario(id.Value);
		ListaRegistros.ItemsSource = registros.Select(Mapear).ToList();
	}

	private static RegistroPontoViewItem Mapear(TimeRecord r)
	{
		var tipo = FormatarTipo(r.Type);
		var quando = FormatarDataHora(r.Timestamp);
		string? local = null;
		if (r.Latitude is { } lat && r.Longitude is { } lon)
			local = $"Local: {lat.ToString("F5", PtBr)}, {lon.ToString("F5", PtBr)}";

		return new RegistroPontoViewItem
		{
			Tipo = tipo,
			DataHora = quando,
			Localizacao = local
		};
	}

	private static string FormatarTipo(string type)
	{
		if (string.Equals(type, "Saida", StringComparison.OrdinalIgnoreCase))
			return "Saída";
		if (string.Equals(type, "Entrada", StringComparison.OrdinalIgnoreCase))
			return "Entrada";
		return type;
	}

	private static string FormatarDataHora(string timestampIso)
	{
		if (!DateTime.TryParse(
			    timestampIso,
			    CultureInfo.InvariantCulture,
			    DateTimeStyles.RoundtripKind,
			    out var instante))
			{
			return timestampIso;
		}

		var local = instante.Kind == DateTimeKind.Unspecified
			? DateTime.SpecifyKind(instante, DateTimeKind.Utc).ToLocalTime()
			: instante.ToLocalTime();

		return local.ToString("dddd, dd/MM/yyyy 'às' HH:mm:ss", PtBr);
	}
}
