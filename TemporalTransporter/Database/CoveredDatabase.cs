using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Database;

public record Covered
{
    public required string CoordinateKey { get; init; }
    public int IsCovered { get; set; }
}

public class CoveredDatabase
{
    private const string CreateTableQuery =
        "CREATE TABLE IF NOT EXISTS Covered (CoordinateKey TEXT PRIMARY KEY, IsCovered INTEGER);";

    private const string InsertQuery =
        "INSERT INTO Covered(CoordinateKey, IsCovered)" +
        "VALUES(@CoordinateKey, 0)";

    private const string GetCoveredQuery =
        "SELECT * FROM Covered WHERE CoordinateKey = @CoordinateKey;";

    private const string SetCoveredQuery =
        "INSERT INTO Covered (CoordinateKey, IsCovered) VALUES (@CoordinateKey, @IsCovered) ON CONFLICT(CoordinateKey) DO UPDATE SET IsCovered = excluded.IsCovered;";

    private const string DeleteCoverForPositionQuery =
        "DELETE FROM Covered WHERE CoordinateKey = @CoordinateKey;";

    private readonly string _connectionString;

    public CoveredDatabase(ICoreAPI api, string modId)
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

    public bool GetIsCovered(Vec3i coords)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(GetCoveredQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(coords));

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return false;
        }

        var isCovered = reader.GetInt32(reader.GetOrdinal("IsCovered"));

        return isCovered != 0;
    }

    public void SetIsCovered(Vec3i coords, bool newValue)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(SetCoveredQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(coords));
        command.Parameters.AddWithValue("@IsCovered", newValue);

        command.ExecuteNonQuery();
    }

    public void DeleteChargeTrackingForPosition(Vec3i position)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(DeleteCoverForPositionQuery, connection);
        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(position));

        command.ExecuteNonQuery();
    }
}