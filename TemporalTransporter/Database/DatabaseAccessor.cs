using System;
using System.Numerics;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Database;

public static class DatabaseAccessor
{
    // initialized during server start. if this causes issues, it is for a reason.
    private static TransporterDatabase? _transporterDatabase;
    private static InterceptorDatabase? _interceptorDatabase;
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

    public static InterceptorDatabase Interceptor
    {
        get
        {
            if (TemporalTransporterModSystem.ServerApi == null)
            {
                throw new InvalidOperationException(
                    "Tried to access Interceptor database from client side or before server initialization.");
            }

            if (_interceptorDatabase == null)
            {
                throw new InvalidOperationException("Interceptor database has not been initialized.");
            }

            return _interceptorDatabase;
        }
        set => _interceptorDatabase = value;
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

    public static Vec3d CoordinateKeyToVec3d(string coordinateKey)
    {
        var vec3i = CoordinateKeyToVec3i(coordinateKey);

        return new Vec3d(vec3i.X, vec3i.Y, vec3i.Z);
    }

    public static Vec3i CoordinateKeyToVec3i(string coordinateKey)
    {
        var coords = coordinateKey.Split(':');
        if (coords.Length != 3)
        {
            throw new ArgumentException("Invalid coordinate key format. Expected format: 'x:y:z'.");
        }

        if (!int.TryParse(coords[0], out var x) || !int.TryParse(coords[1], out var y) ||
            !int.TryParse(coords[2], out var z))
        {
            throw new ArgumentException("Coordinate key contains non-integer values.");
        }

        return new Vec3i(x, y, z);
    }

    public static Vector3 CoordinateKeyToVector3(string coordinateKey)
    {
        var (x, y, z) = ParseCoords(coordinateKey);

        return new Vector3(x, y, z);
    }

    private static (float x, float y, float z) ParseCoords(string coordinateKey)
    {
        var coords = coordinateKey.Split(':');
        if (coords.Length != 3)
        {
            throw new ArgumentException("Invalid coordinate key format. Expected format: 'x:y:z'.");
        }

        if (!float.TryParse(coords[0], out var x) || !float.TryParse(coords[1], out var y) ||
            !float.TryParse(coords[2], out var z))
        {
            throw new ArgumentException("Coordinate key contains non-float values.");
        }

        return (x, y, z);
    }
}