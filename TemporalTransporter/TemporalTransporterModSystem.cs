using TemporalTransporter.Database;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TemporalTransporter;

public class TemporalTransporterModSystem : ModSystem
{
    private TransporterDatabase _transporterDatabase;

    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockEntityClass("TemporalTransporter", typeof(BlockEntityTemporalTransporter));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        _transporterDatabase = new TransporterDatabase(api, Mod.Info.ModID);
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
    }
}