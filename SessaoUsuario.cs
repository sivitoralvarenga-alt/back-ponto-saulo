namespace MyMauiApp;

public static class SessaoUsuario
{
	private const string ChaveUsuarioId = "usuario_atual_id";

	public static int? ObterId()
	{
		var id = Preferences.Get(ChaveUsuarioId, 0);
		return id <= 0 ? null : id;
	}

	public static void DefinirAtual(int userId) => Preferences.Set(ChaveUsuarioId, userId);

	public static void Limpar() => Preferences.Remove(ChaveUsuarioId);
}
