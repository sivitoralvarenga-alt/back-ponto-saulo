using PontoSaulo.Data;
using PontoSaulo.Models;

namespace MyMauiApp;

public partial class UsuariosPage : ContentPage
{
	public UsuariosPage()
	{
		InitializeComponent();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		CarregarLista();
	}

	private void CarregarLista()
	{
		var db = new DatabaseService();
		var usuarios = db.ListarUsuarios();

		ListaUsuarios.SelectionChanged -= OnUsuarioSelecionado;
		ListaUsuarios.ItemsSource = usuarios;

		var idAtual = SessaoUsuario.ObterId();
		if (idAtual.HasValue)
			ListaUsuarios.SelectedItem = usuarios.FirstOrDefault(u => u.Id == idAtual.Value);
		else
			ListaUsuarios.SelectedItem = null;

		ListaUsuarios.SelectionChanged += OnUsuarioSelecionado;
	}

	private async void OnUsuarioSelecionado(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not User user)
			return;

		var pin = await DisplayPromptAsync(
			"Confirmar PIN",
			$"Digite o PIN de {user.Nome}:",
			"OK",
			"Cancelar",
			placeholder: "PIN",
			maxLength: 32,
			keyboard: Keyboard.Numeric);

		if (string.IsNullOrEmpty(pin))
		{
			ListaUsuarios.SelectedItem = null;
			return;
		}

		var db = new DatabaseService();
		if (!db.ValidarPin(user.Id, pin))
		{
			await DisplayAlert("PIN incorreto", "Tente novamente ou escolha outro usuário.", "OK");
			ListaUsuarios.SelectedItem = null;
			return;
		}

		SessaoUsuario.DefinirAtual(user.Id);
		await DisplayAlert("Usuário ativo", $"{user.Nome} está registrando o ponto.", "OK");
	}

	private async void OnNovoUsuarioClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new CadastroUsuarioPage());
	}
}
