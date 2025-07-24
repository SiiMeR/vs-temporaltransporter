using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Database;

public record Interceptor
{
    public required string CoordinateKey { get; init; }
}

public class InterceptorDatabase
{
    private const string CreateTableQuery =
        "CREATE TABLE IF NOT EXISTS Interceptors (CoordinateKey TEXT PRIMARY KEY);";

    private const string InsertInterceptorQuery =
        "INSERT INTO Interceptors (CoordinateKey) VALUES (@CoordinateKey);";

    private const string GetInterceptorQuery =
        "SELECT * FROM Interceptors WHERE CoordinateKey = @CoordinateKey;";

    private const string GetAllInterceptorsQuery =
        "SELECT * FROM Interceptors;";


    private const string DeleteInterceptorQuery =
        "DELETE FROM Interceptors WHERE CoordinateKey = @CoordinateKey;";

    private readonly string _connectionString;

    public InterceptorDatabase(ICoreAPI api, string modId)
    {
        var databaseDirectory = Path.Combine(GamePaths.DataPath, "ModData",
            api.World.SavegameIdentifier,
            modId);

        var path = Path.Combine(databaseDirectory, DatabaseAccessor.DatabaseName);
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

    public void RemoveInterceptorByPosition(Vec3i position)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(DeleteInterceptorQuery, connection);
        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(position));

        command.ExecuteNonQuery();
    }

    public Interceptor[] GetAllInterceptors()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(GetAllInterceptorsQuery, connection);

        using var reader = command.ExecuteReader();
        var interceptors = new List<Interceptor>();

        while (reader.Read())
        {
            var coordinateKey = (string)reader["CoordinateKey"];
            interceptors.Add(new Interceptor { CoordinateKey = coordinateKey });
        }

        return interceptors.ToArray();
    }

    public void InsertInterceptor(Interceptor interceptor)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(InsertInterceptorQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", interceptor.CoordinateKey);

        command.ExecuteScalar();
    }

    public Interceptor? GetInterceptor(Vec3i coords)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(GetInterceptorQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(coords));

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var coordinateKey = (string)reader["CoordinateKey"];

        return new Interceptor
        {
            CoordinateKey = coordinateKey
        };
    }
}