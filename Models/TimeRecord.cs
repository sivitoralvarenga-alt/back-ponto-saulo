namespace PontoSaulo.Models;

public sealed class TimeRecord
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public string Timestamp { get; set; } = string.Empty;
	public double? Latitude { get; set; }
	public double? Longitude { get; set; }
	public string Type { get; set; } = string.Empty;
	public string AuthMethod { get; set; } = string.Empty;

	/// <summary>Nome da coluna no SQLite: sucess (mantido conforme o esquema).</summary>
	public bool Sucess { get; set; }
}
