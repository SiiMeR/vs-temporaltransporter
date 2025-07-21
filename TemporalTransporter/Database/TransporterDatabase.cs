using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Database;

public record Transporter
{
    public required string CoordinateKey { get; init; }
    public string? ConnectionKey { get; set; }
}

public class TransporterDatabase
{
    private const string CreateTableQuery =
        "CREATE TABLE IF NOT EXISTS Transporters (CoordinateKey TEXT PRIMARY KEY, ConnectionKey TEXT);";

    private const string InsertTransporterQuery =
        "INSERT INTO Transporters (CoordinateKey) VALUES (@CoordinateKey)" +
        "ON CONFLICT(CoordinateKey) DO UPDATE SET ConnectionKey = excluded.ConnectionKey;";

    private const string GetTransporterQuery =
        "SELECT * FROM Transporters WHERE CoordinateKey = @CoordinateKey;";


    private const string DeleteTransporterQuery =
        "DELETE FROM Transporters WHERE CoordinateKey = @CoordinateKey;";

    private const string GetTransporterConnectionKeyQuery =
        "SELECT * FROM Transporters WHERE ConnectionKey = @ConnectionKey;";

    private const string SetTransporterConnectionKeyQuery =
        "UPDATE Transporters SET ConnectionKey = @ConnectionKey WHERE CoordinateKey = @CoordinateKey;";

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

    public void RemoveTransporterByPosition(Vec3i position)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(DeleteTransporterQuery, connection);
        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(position));

        command.ExecuteNonQuery();
    }

    public Transporter[] GetTransportersByConnectionKey(string connectionKey)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(GetTransporterConnectionKeyQuery, connection);
        command.Parameters.AddWithValue("@ConnectionKey", connectionKey);

        using var reader = command.ExecuteReader();
        var transporters = new List<Transporter>();

        while (reader.Read())
        {
            var coordinateKey = (string)reader["CoordinateKey"];
            var transporter = new Transporter
            {
                CoordinateKey = coordinateKey,
                ConnectionKey = connectionKey
            };
            transporters.Add(transporter);
        }

        return transporters.ToArray();
    }

    public void SetTransporterConnectionKey(Vec3i position, string connectionKey)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(SetTransporterConnectionKeyQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(position));
        command.Parameters.AddWithValue("@ConnectionKey", connectionKey);

        command.ExecuteNonQuery();
    }


    public void InsertTransporter(Transporter transporter)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(InsertTransporterQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", transporter.CoordinateKey);

        command.ExecuteScalar();
    }

    public Transporter? GetTransporter(Vec3i coords)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(GetTransporterQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(coords));

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var coordinateKey = (string)reader["CoordinateKey"];

        return new Transporter
        {
            CoordinateKey = coordinateKey
        };
    }
}