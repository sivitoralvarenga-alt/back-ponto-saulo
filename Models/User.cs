namespace PontoSaulo.Models;

public sealed class User
{
	public int Id { get; set; }
	public string Nome { get; set; } = string.Empty;
	public string? Email { get; set; }
	public string? Telefone { get; set; }
	public string SecreteCodeHash { get; set; } = string.Empty;
	public string? Facedata { get; set; }
	public string CreatedAt { get; set; } = string.Empty;
}
