using Microsoft.Data.Sqlite;
using PontoSaulo.Models;

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
		@"
		INSERT INTO TimeRecord
		(userId, timestamp, latitude, longitude, type, authMethod, sucess)
		VALUES
		($userId, $timestamp, $latitude, $longitude, $type, $authMethod, $sucess);
		";

		cmd.Parameters.AddWithValue("$userId", record.UserId);
		cmd.Parameters.AddWithValue("$timestamp", record.Timestamp);
		cmd.Parameters.AddWithValue("$latitude", (object?)record.Latitude ?? DBNull.Value);
		cmd.Parameters.AddWithValue("$longitude", (object?)record.Longitude ?? DBNull.Value);
		cmd.Parameters.AddWithValue("$type", record.Type);
		cmd.Parameters.AddWithValue("$authMethod", record.AuthMethod);
		cmd.Parameters.AddWithValue("$sucess", record.Sucess ? 1 : 0);

		cmd.ExecuteNonQuery();
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
