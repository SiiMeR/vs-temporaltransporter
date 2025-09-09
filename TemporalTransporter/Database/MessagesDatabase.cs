using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace TemporalTransporter.Database;

public record Message
{
    public string? Id { get; set; }
    public required string FromCoordinateKey { get; init; }
    public required string ToCoordinateKey { get; init; }
    public required byte[]? ItemBlob { get; init; }
    public required string SendDate { get; init; }
    public required string ByPlayerUID { get; init; }
}

public class MessagesDatabase
{
    private const string CreateTableQuery =
        "CREATE TABLE IF NOT EXISTS Messages (Id TEXT PRIMARY KEY, FromCoordinateKey TEXT, ToCoordinateKey TEXT, ItemBlob BLOB, SendDate TEXT, ByPlayerUID TEXT);";

    private const string InsertMessageQuery =
        "INSERT INTO Messages(Id, FromCoordinateKey, ToCoordinateKey, ItemBlob, SendDate, ByPlayerUID)" +
        "VALUES(newguid(), @FromCoordinateKey, @ToCoordinateKey, @ItemBlob, @SendDate, @ByPlayerUID)";


    private readonly string _connectionString;

    public MessagesDatabase(ICoreAPI api, string modId)
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

    public void InsertMessage(Message message)
    {
        using var connection = CreateConnection();

        using var command = new SqliteCommand(InsertMessageQuery, connection);

        command.Parameters.AddWithValue("@FromCoordinateKey", message.FromCoordinateKey);
        command.Parameters.AddWithValue("@ToCoordinateKey", message.ToCoordinateKey);
        command.Parameters.AddWithValue("@ItemBlob", message.ItemBlob ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SendDate", message.SendDate);
        command.Parameters.AddWithValue("@ByPlayerUID", message.ByPlayerUID);

        command.ExecuteScalar();
    }

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        connection.CreateFunction("newguid", () => Guid.NewGuid().ToString("n"));

        return connection;
    }
}