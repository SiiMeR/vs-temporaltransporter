using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace TemporalTransporter.Database;

public record Transporter
{
    public long Id { get; set; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required int Z { get; init; }
}

public class TransporterDatabase
{
    private const string CreateTableQuery =
        "CREATE TABLE IF NOT EXISTS Transporters (Id INTEGER PRIMARY KEY, X INTEGER, Y INTEGER, Z INTEGER);";

    private const string InsertTransporterQuery =
        "INSERT INTO Transporters (X, Y, Z) VALUES (@X, @Y, @Z); SELECT last_insert_rowid();";

    private const string GetTransporterQuery =
        "SELECT Id, X, Y, Z FROM Transporters WHERE Id = @Id;";

    private readonly string _connectionString;

    public TransporterDatabase(ICoreAPI api, string modId)
    {
        var databaseDirectory = Path.Combine(GamePaths.DataPath, "ModData",
            api.World.SavegameIdentifier,
            modId);

        var path = Path.Combine(databaseDirectory, "Transporters.db");
        _connectionString = $"Data Source={path};";

        if (!Directory.Exists(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        using var connection = CreateConnection();
        new SqliteCommand(CreateTableQuery, connection).ExecuteNonQuery();
    }

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        return connection;
    }

    public long InsertTransporter(Transporter transporter)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(InsertTransporterQuery, connection);

        command.Parameters.AddWithValue("@X", transporter.X);
        command.Parameters.AddWithValue("@Y", transporter.Y);
        command.Parameters.AddWithValue("@Z", transporter.Z);

        var result = command.ExecuteScalar();
        if (result == null)
        {
            throw new InvalidOperationException("Failed to insert a new Transporter.");
        }

        var transporterId = (long)result;

        return transporterId;
    }

    public Transporter? GetTransporter(long id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(GetTransporterQuery, connection);
        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var identifier = (long)reader["Id"];
        var x = (int)reader["X"];
        var y = (int)reader["Y"];
        var z = (int)reader["Z"];

        return new Transporter
        {
            Id = identifier,
            X = x,
            Y = y,
            Z = z
        };
    }
}