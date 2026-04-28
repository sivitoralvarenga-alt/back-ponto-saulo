using Microsoft.Data.Sqlite;
using PontoSaulo.Data;

namespace MyMauiApp;

public partial class CadastroUsuarioPage : ContentPage
{
	public CadastroUsuarioPage()
	{
		InitializeComponent();
	}

	private string? _fotoBase64;


	private async void TirarFoto(object? sender, EventArgs e)
	{
		try
		{
			if(MediaPicker.Default.IsCaptureSupported)
			{
				var foto = await MediaPicker.Default.CapturePhotoAsync();
				

				if (foto != null)
				{
					using var stream = await foto.OpenReadAsync();
					using var ms = new MemoryStream();

					await stream.CopyToAsync(ms);

					byte[] bytes = ms.ToArray();

					_fotoBase64 = Convert.ToBase64String(bytes);

					FotoPreview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                	FotoPreview.IsVisible = true;
                	IconeCamera.IsVisible = false;
				}
			}
		}
		catch (Exception ex)
		{
		}
	}

	private async void OnSalvarClicked(object? sender, EventArgs e)
	{
		var nome = NomeEntry.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrEmpty(nome))
		{
			await DisplayAlert("Validação", "Informe o nome.", "OK");
			return;
		}

		var pin = PinEntry.Text ?? string.Empty;
		if (pin.Length < 4)
		{
			await DisplayAlert("Validação", "O PIN deve ter pelo menos 4 caracteres.", "OK");
			return;
		}

		if (pin != ConfirmarPinEntry.Text)
		{
			await DisplayAlert("Validação", "Os PINs não coincidem.", "OK");
			return;
		}

        try
		{
			var db = new DatabaseService();
			var email = EmailEntry.Text;
			var id = db.CriarUsuario(nome, string.IsNullOrWhiteSpace(email) ? null : email, pin, _fotoBase64);
			SessaoUsuario.DefinirAtual(id);
			await DisplayAlert("Sucesso", "Usuário cadastrado e definido como ativo.", "OK");
			await Navigation.PopAsync();
		}
		catch (SqliteException ex) when (ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
		{
			// Could be duplicate email or duplicate name because of UNIQUE constraint on nome and email
			await DisplayAlert("Cadastro", "Já existe um usuário com este nome ou e-mail.", "OK");
		}
	}
}
