using PontoSaulo.Data;

namespace MyMauiApp;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnEntrarClicked(object? sender, EventArgs e)
    {
        var nome = NomeEntry.Text?.Trim() ?? string.Empty;
        var pin = PinEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(nome))
        {
            await DisplayAlert("Validação", "Informe o nome.", "OK");
            return;
        }

        var db = new DatabaseService();
        var usuario = db.ObterUsuarioPorNome(nome);
        if (usuario == null)
        {
            await DisplayAlert("Usuário", "Usuário não encontrado. Cadastre-se.", "OK");
            return;
        }

        if (!db.ValidarPin(usuario.Id, pin))
        {
            await DisplayAlert("PIN incorreto", "PIN inválido.", "OK");
            return;
        }

        SessaoUsuario.DefinirAtual(usuario.Id);
        await Navigation.PopModalAsync();
    }

    private async void OnCadastrarClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new CadastroUsuarioPage());
    }
}
