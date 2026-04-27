using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using PontoSaulo.Models;
using static System.DBNull;

namespace PontoSaulo.Data;

public sealed class DatabaseService
{
	public string DatabasePath { get; }

	public DatabaseService()
	{
		DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "ponto.sqlite");
	}

	public IReadOnlyList<(TimeRecord Record, string UserName)> ListarTodosRegistros()
	{
		var builder = new SqliteConnectionStringBuilder { DataSource = DatabasePath };
		using var connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText =
			"""
			SELECT tr.id, tr.userId, tr.timestamp, tr.latitude, tr.longitude, tr.type, tr.authMethod, tr.sucess, u.nome
			FROM TimeRecords tr
			JOIN Users u ON tr.userId = u.id
			ORDER BY tr.timestamp DESC;
			""";

		var lista = new List<(TimeRecord, string)>();
		using (var reader = cmd.ExecuteReader())
		{
			while (reader.Read())
			{
				var record = new TimeRecord
				{
					Id = reader.GetInt32(0),
					UserId = reader.GetInt32(1),
					Timestamp = reader.GetString(2),
					Latitude = reader.IsDBNull(3) ? null : reader.GetDouble(3),
					Longitude = reader.IsDBNull(4) ? null : reader.GetDouble(4),
					Type = reader.GetString(5),
					AuthMethod = reader.GetString(6),
					Sucess = reader.GetInt32(7) != 0
				};

				var nome = reader.IsDBNull(8) ? string.Empty : reader.GetString(8);
				lista.Add((record, nome));
			}
		}

		return lista;
	}

	public static string HashPin(string pin)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pin));
		return Convert.ToHexString(bytes);
	}

	public void Initialize()
	{
		var directory = Path.GetDirectoryName(DatabasePath);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		var builder = new SqliteConnectionStringBuilder
		{
			DataSource = DatabasePath,
			Mode = SqliteOpenMode.ReadWriteCreate
		};

		using var connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		using (var pragma = connection.CreateCommand())
		{
			pragma.CommandText = "PRAGMA foreign_keys = ON;";
			pragma.ExecuteNonQuery();
		}

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = SqlSchema.UsersTable;
			cmd.ExecuteNonQuery();
		}

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = SqlSchema.TimeRecordsTable;
			cmd.ExecuteNonQuery();
		}

		MigrateUsersAddTelefoneIfMissing(connection);
	}

	private static void MigrateUsersAddTelefoneIfMissing(SqliteConnection connection)
	{
		using var check = connection.CreateCommand();
		check.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Users') WHERE name = 'telefone';";
		var exists = Convert.ToInt64(check.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
		if (exists)
			return;

		using var alter = connection.CreateCommand();
		alter.CommandText = "ALTER TABLE Users ADD COLUMN telefone TEXT;";
		alter.ExecuteNonQuery();
	}

	public void InsertPonto(TimeRecord record)
	{
		var builder = new SqliteConnectionStringBuilder
		{
			DataSource = DatabasePath
		};

		using var connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();

		cmd.CommandText =
	"""
		INSERT INTO TimeRecords
		(userId, timestamp, latitude, longitude, type, authMethod, sucess)
		VALUES
		($userId, $timestamp, $latitude, $longitude, $type, $authMethod, $sucess);
		""";

		cmd.Parameters.AddWithValue("$userId", record.UserId);
		cmd.Parameters.AddWithValue("$timestamp", record.Timestamp);
		cmd.Parameters.AddWithValue("$latitude", (object?)record.Latitude ?? DBNull.Value);
		cmd.Parameters.AddWithValue("$longitude", (object?)record.Longitude ?? DBNull.Value);
		cmd.Parameters.AddWithValue("$type", record.Type);
		cmd.Parameters.AddWithValue("$authMethod", record.AuthMethod);
		cmd.Parameters.AddWithValue("$sucess", record.Sucess ? 1 : 0);

		cmd.ExecuteNonQuery();
	}

	public IReadOnlyList<User> ListarUsuarios()
	{
		var builder = new SqliteConnectionStringBuilder { DataSource = DatabasePath };
		using var connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText =
			"""
			SELECT id, nome, email, telefone, secreteCodeHash, facedata, createdAt
			FROM Users
			ORDER BY nome COLLATE NOCASE;
			""";

		var lista = new List<User>();
		using (var reader = cmd.ExecuteReader())
		{
			while (reader.Read())
				lista.Add(MapUser(reader));
		}

		return lista;
	}

	public User? ObterUsuario(int id)
	{
		var builder = new SqliteConnectionStringBuilder { DataSource = DatabasePath };
		using var connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText =
			"""
			SELECT id, nome, email, telefone, secreteCodeHash, facedata, createdAt
			FROM Users
			WHERE id = $id
			LIMIT 1;
			""";
		cmd.Parameters.AddWithValue("$id", id);

		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
			return null;

		return MapUser(reader);
	}

	public int CriarUsuario(string nome, string? email, string? telefone, string pin, string? faceData)
	{
		var agora = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
		var hash = HashPin(pin);

		var builder = new SqliteConnectionStringBuilder { DataSource = DatabasePath };
		using var connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText =
				"""
				INSERT INTO Users (nome, email, telefone, secreteCodeHash, facedata, createdAt)
				VALUES ($nome, $email, $telefone, $hash, $face, $createdAt);
				""";

			cmd.Parameters.AddWithValue("$nome", nome.Trim());
			cmd.Parameters.AddWithValue("$email", string.IsNullOrWhiteSpace(email) ? DBNull.Value : email.Trim());
			cmd.Parameters.AddWithValue("$telefone", string.IsNullOrWhiteSpace(telefone) ? DBNull.Value : telefone.Trim());
			cmd.Parameters.AddWithValue("$hash", hash);
			cmd.Parameters.AddWithValue("$face", (object?)faceData ?? DBNull.Value);
			cmd.Parameters.AddWithValue("$createdAt", agora);
			cmd.ExecuteNonQuery();
		}

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = "SELECT last_insert_rowid();";
			var scalar = cmd.ExecuteScalar();
			return Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
		}
	}

	public User? ObterUsuarioPorNome(string nome)
	{
		var builder = new SqliteConnectionStringBuilder { DataSource = DatabasePath };
		using var connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText =
			"""
			SELECT id, nome, email, telefone, secreteCodeHash, facedata, createdAt
			FROM Users
			WHERE nome = $nome
			LIMIT 1;
			""";
		cmd.Parameters.AddWithValue("$nome", nome.Trim());

		using var reader = cmd.ExecuteReader();
		if (!reader.Read())
			return null;

		return MapUser(reader);
	}

	private static User MapUser(SqliteDataReader reader)
	{
		var ordTelefone = reader.GetOrdinal("telefone");
		return new User
		{
			Id = reader.GetInt32(reader.GetOrdinal("id")),
			Nome = reader.GetString(reader.GetOrdinal("nome")),
			Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
			Telefone = reader.IsDBNull(ordTelefone) ? null : reader.GetString(ordTelefone),
			SecreteCodeHash = reader.GetString(reader.GetOrdinal("secreteCodeHash")),
			Facedata = reader.IsDBNull(reader.GetOrdinal("facedata")) ? null : reader.GetString(reader.GetOrdinal("facedata")),
			CreatedAt = reader.GetString(reader.GetOrdinal("createdAt"))
		};
	}

	public bool ValidarPin(int userId, string pin)
	{
		var usuario = ObterUsuario(userId);
		if (usuario == null)
			return false;
		return string.Equals(usuario.SecreteCodeHash, HashPin(pin), StringComparison.Ordinal);
	}

	public IReadOnlyList<TimeRecord> ListarRegistrosDoUsuario(int userId)
	{
		var builder = new SqliteConnectionStringBuilder { DataSource = DatabasePath };
		using var connection = new SqliteConnection(builder.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText =
			"""
			SELECT id, userId, timestamp, latitude, longitude, type, authMethod, sucess
			FROM TimeRecords
			WHERE userId = $userId
			ORDER BY timestamp DESC;
			""";
		cmd.Parameters.AddWithValue("$userId", userId);

		var lista = new List<TimeRecord>();
		using (var reader = cmd.ExecuteReader())
		{
			while (reader.Read())
			{
				lista.Add(new TimeRecord
				{
					Id = reader.GetInt32(0),
					UserId = reader.GetInt32(1),
					Timestamp = reader.GetString(2),
					Latitude = reader.IsDBNull(3) ? null : reader.GetDouble(3),
					Longitude = reader.IsDBNull(4) ? null : reader.GetDouble(4),
					Type = reader.GetString(5),
					AuthMethod = reader.GetString(6),
					Sucess = reader.GetInt32(7) != 0
				});
			}
		}

		return lista;
	}
}

internal static class SqlSchema
{
	/// <summary>Users deve existir antes de TimeRecords (FK).</summary>
  internal const string UsersTable = @"CREATE TABLE IF NOT EXISTS Users (
		id INTEGER PRIMARY KEY AUTOINCREMENT,
		nome TEXT NOT NULL UNIQUE,
		email TEXT UNIQUE,
		telefone TEXT,
		secreteCodeHash TEXT NOT NULL,
		facedata TEXT,
		createdAt TEXT NOT NULL
	);";

	internal const string TimeRecordsTable =
		"""
		CREATE TABLE IF NOT EXISTS TimeRecords(
		    id INTEGER PRIMARY KEY AUTOINCREMENT,
		    userId INTEGER NOT NULL,
		    timestamp TEXT NOT NULL,
		    latitude REAL,
		    longitude REAL,
		    type TEXT NOT NULL,
		    authMethod TEXT NOT NULL,
		    sucess BOOLEAN NOT NULL,
		    FOREIGN KEY (userId) REFERENCES Users(id)
		);
		""";
}
