using System;

namespace TemporalTransporter.Database;

public static class DatabaseAccessor
{
    // initialized during server start. if this causes issues, it is for a reason.
    private static TransporterDatabase? _transporterDatabase;

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
}