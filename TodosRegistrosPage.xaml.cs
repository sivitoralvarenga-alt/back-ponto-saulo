using System.Globalization;
using PontoSaulo.Data;
using PontoSaulo.Models;

namespace MyMauiApp;

public sealed class TodosRegistrosViewItem
{
	public string UserName { get; init; } = "";
	public TimeRecord Record { get; init; } = new TimeRecord();
	public string Localizacao { get; init; } = "";

	/// <summary>Texto único para binding (MAUI não concatena dois {Binding} no mesmo atributo).</summary>
	public string TituloResumo => $"{UserName} — {Record.Type}";
}

public partial class TodosRegistrosPage : ContentPage
{
	private static readonly CultureInfo PtBr = new("pt-BR");

	public TodosRegistrosPage()
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
		if (Navigation.NavigationStack.Count > 1)
			await Navigation.PopAsync();
	}

	private void Carregar()
	{
		var db = new DatabaseService();
		var lista = db.ListarTodosRegistros();
		ListaRegistros.ItemsSource = lista.Select(Mapear).ToList();
		LabelContexto.Text = $"Total: {lista.Count} registros. Mais recentes primeiro.";
	}

	private static TodosRegistrosViewItem Mapear((TimeRecord Record, string UserName) item)
	{
		var r = item.Record;
		string local = string.Empty;
		if (r.Latitude is { } lat && r.Longitude is { } lon)
			local = $"Local: {lat.ToString("F5", PtBr)}, {lon.ToString("F5", PtBr)}";

		return new TodosRegistrosViewItem
		{
			UserName = item.UserName,
			Record = r,
			Localizacao = local
		};
	}
}
