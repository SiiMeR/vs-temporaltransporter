using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Database;

public record InventoryItem
{
    public required string CoordinateKey { get; init; }
    public required int SlotId { get; init; }
    public byte[]? ItemBlob { get; set; }
}

public class InventoryItemDatabase
{
    private const string CreateTableQuery =
        "CREATE TABLE IF NOT EXISTS InventoryItems (CoordinateKey TEXT, SlotId INTEGER, ItemBlob BLOB, PRIMARY KEY (CoordinateKey, SlotId))";

    private const string InsertInventoryItemQuery =
        "INSERT INTO InventoryItems (CoordinateKey, SlotId, ItemBlob) VALUES (@CoordinateKey, @SlotId, @ItemBlob) " +
        "ON CONFLICT(CoordinateKey, SlotId) DO UPDATE SET ItemBlob = excluded.ItemBlob;";

    private const string UpdateInventoryItemSlotQuery =
        "UPDATE InventoryItems SET ItemBlob = @ItemBlob WHERE CoordinateKey = @CoordinateKey AND SlotId = @SlotId;";

    private const string GetInventoryItemQuery =
        "SELECT * FROM InventoryItems WHERE CoordinateKey = @CoordinateKey AND SlotId = @SlotId;";

    private const string DeleteInventoryItemQuery =
        "DELETE FROM InventoryItems WHERE CoordinateKey = @CoordinateKey AND SlotId = @SlotId;";


    private const string DeleteInventoryItemsByCoordinateQuery =
        "DELETE FROM InventoryItems WHERE CoordinateKey = @CoordinateKey;";

    private const string GetInventoryItemsByCoordinateQuery =
        "SELECT * FROM InventoryItems WHERE CoordinateKey = @CoordinateKey;";

    private readonly string _connectionString;

    public InventoryItemDatabase(ICoreAPI api, string modId)
    {
        var databaseDirectory = Path.Combine(GamePaths.DataPath, "ModData",
            api.World.SavegameIdentifier,
            modId);

        var path = Path.Combine(databaseDirectory, "InventoryItems.db");
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

    public InventoryItem[] GetInventoryItems(string coordinateKey)
    {
        using var connection = CreateConnection();
        using var command = new SqliteCommand(GetInventoryItemsByCoordinateQuery, connection);
        command.Parameters.AddWithValue("@CoordinateKey", coordinateKey);

        using var reader = command.ExecuteReader();
        var items = new List<InventoryItem>();

        while (reader.Read())
        {
            var item = new InventoryItem
            {
                CoordinateKey = reader.GetString(0),
                SlotId = reader.GetInt32(1),
                ItemBlob = reader.IsDBNull(2) ? null : (byte[])reader[2]
            };
            items.Add(item);
        }

        return items.ToArray();
    }

    public int GetFirstFreeSlotId(string coordinateKey)
    {
        using var connection = CreateConnection();
        using var command = new SqliteCommand(GetInventoryItemsByCoordinateQuery, connection);
        command.Parameters.AddWithValue("@CoordinateKey", coordinateKey);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var slotId = reader.GetInt32(1);
            var itemBlob = reader.IsDBNull(2) ? null : reader.GetString(2);

            if (string.IsNullOrWhiteSpace(itemBlob))
            {
                return slotId;
            }
        }

        return -1;
    }

    public void InsertInventoryItem(InventoryItem inventoryItem)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = new SqliteCommand(InsertInventoryItemQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", inventoryItem.CoordinateKey);
        command.Parameters.AddWithValue("@SlotId", inventoryItem.SlotId);
        command.Parameters.AddWithValue("@ItemBlob", inventoryItem.ItemBlob ?? (object)DBNull.Value);


        command.ExecuteScalar();
    }

    public void UpdateInventoryItemSlot(string coordinateKey, int slotId, byte[]? itemBlob)
    {
        using var connection = CreateConnection();
        using var command = new SqliteCommand(UpdateInventoryItemSlotQuery, connection);

        command.Parameters.AddWithValue("@CoordinateKey", coordinateKey);
        command.Parameters.AddWithValue("@SlotId", slotId);
        command.Parameters.AddWithValue("@ItemBlob", itemBlob ?? (object)DBNull.Value);

        command.ExecuteNonQuery();
    }

    public void InitializeInventoryForPosition(Vec3i position)
    {
        using var connection = CreateConnection();

        for (var slotId = 0; slotId < 8; slotId++)
        {
            var inventoryItem = new InventoryItem
            {
                CoordinateKey = DatabaseAccessor.GetCoordinateKey(position),
                SlotId = slotId,
                ItemBlob = null
            };

            InsertInventoryItem(inventoryItem);
        }
    }

    public void ClearInventoryForPosition(Vec3i position)
    {
        using var connection = CreateConnection();
        using var command = new SqliteCommand(DeleteInventoryItemsByCoordinateQuery, connection);
        command.Parameters.AddWithValue("@CoordinateKey", DatabaseAccessor.GetCoordinateKey(position));
        command.ExecuteNonQuery();
    }
}