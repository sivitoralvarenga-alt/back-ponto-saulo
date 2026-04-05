using Microsoft.Data.Sqlite;

namespace PontoSaulo.Data;

public sealed class DatabaseService
{
	public string DatabasePath { get; }

	public DatabaseService()
	{
		DatabasePath = Path.Combine(FileSystem.AppDataDirectory, "ponto.sqlite");
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
	}
}

internal static class SqlSchema
{
	/// <summary>Users deve existir antes de TimeRecords (FK).</summary>
	internal const string UsersTable =
		"""
		CREATE TABLE IF NOT EXISTS Users (
		    id INTEGER PRIMARY KEY AUTOINCREMENT,
		    nome TEXT NOT NULL,
		    email TEXT UNIQUE,
		    secreteCodeHash TEXT NOT NULL,
		    facedata TEXT,
		    createdAt TEXT NOT NULL
		);
		""";

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
