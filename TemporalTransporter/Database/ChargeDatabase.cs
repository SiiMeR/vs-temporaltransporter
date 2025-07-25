using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Database;

public record Charge
{
    public required string CoordinateKey { get; init; }
    public int ChargeCount { get; set; }
}

public class ChargeDatabase
{
    private const string CreateTableQuery =
        "CREATE TABLE IF NOT EXISTS Charges (CoordinateKey TEXT PRIMARY KEY, ChargeCount INTEGER);";

    private const string InsertChargeQuery =
        "INSERT INTO Charges(CoordinateKey, ChargeCount)" +
        "VALUES(@CoordinateKey, 0)";

    private const string IncrementChargeQuery =
        "UPDATE Charges SET ChargeCount = ChargeCount + 1 WHERE CoordinateKey = @CoordinateKey;";

    private const string DecrementChargeQuery =
        "UPDATE Charges SET ChargeCount = ChargeCount - 1 WHERE CoordinateKey = @CoordinateKey;";

    private const string GetChargeQuery =
        "SELECT * FROM Charges WHERE CoordinateKey = @CoordinateKey;";

    private const string DeleteChargeTrackingForPositionQuery =
        "DELETE FROM Charges WHERE CoordinateKey = @CoordinateKey;";

    private readonly string _connectionString;

    public ChargeDatabase(ICoreAPI api, string modId)
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

    public void IncrementCharge(Vec3i coords)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(IncrementChargeQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(coords));

        command.ExecuteNonQuery();
    }

    public void DecrementCharge(Vec3i coords)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(DecrementChargeQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(coords));

        command.ExecuteNonQuery();
    }


    public void InitializeCharges(Vec3i coords)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(InsertChargeQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(coords));

        command.ExecuteScalar();
    }

    public int GetChargeCount(Vec3i coords)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(GetChargeQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(coords));

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return 0;
        }

        var chargeCount = reader.GetInt32(reader.GetOrdinal("ChargeCount"));

        return chargeCount;
    }

    public void DeleteChargeTrackingForPosition(Vec3i position)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(DeleteChargeTrackingForPositionQuery, connection);
        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(position));

        command.ExecuteNonQuery();
    }
}