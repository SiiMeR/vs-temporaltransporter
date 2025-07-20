using System;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Database;

public static class DatabaseAccessor
{
    // initialized during server start. if this causes issues, it is for a reason.
    private static TransporterDatabase? _transporterDatabase;
    private static InventoryItemDatabase? _inventoryItemDatabase;

    public static TransporterDatabase Transporter
    {
        get
        {
            if (TemporalTransporterModSystem.ServerApi == null)
            {
                throw new InvalidOperationException(
                    "Tried to access Transporter database from client side or before server initialization.");
            }

            if (_transporterDatabase == null)
            {
                throw new InvalidOperationException("Transporter database has not been initialized.");
            }

            return _transporterDatabase;
        }
        set => _transporterDatabase = value;
    }

    public static InventoryItemDatabase InventoryItem
    {
        get
        {
            if (TemporalTransporterModSystem.ServerApi == null)
            {
                throw new InvalidOperationException(
                    "Tried to access InventoryItem database from client side or before server initialization.");
            }

            if (_inventoryItemDatabase == null)
            {
                throw new InvalidOperationException("InventoryItem database has not been initialized.");
            }

            return _inventoryItemDatabase;
        }
        set => _inventoryItemDatabase = value;
    }


    public static string GetCoordinateKey(int x, int y, int z)
    {
        return $"{x}:{y}:{z}";
    }

    public static string GetCoordinateKey(Vec3i position)
    {
        return GetCoordinateKey(position.X, position.Y, position.Z);
    }
}