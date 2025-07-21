using TemporalTransporter.Behaviors;
using TemporalTransporter.Database;
using TemporalTransporter.Entities;
using TemporalTransporter.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TemporalTransporter;

public class TemporalTransporterModSystem : ModSystem
{
    public static ICoreServerAPI? ServerApi;
    public static ICoreClientAPI? ClientApi;

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemTransporterKey", typeof(ItemTransporterKey));
        api.RegisterBlockEntityClass("TemporalTransporter", typeof(BlockEntityTemporalTransporter));
        api.RegisterBlockEntityBehaviorClass("Uncoverable", typeof(BlockEntityBehaviorUncoverable));
        api.RegisterBlockEntityBehaviorClass("Skybeam", typeof(BlockEntityBehaviorSkyBeam));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        ServerApi = api;
        DatabaseAccessor.Transporter = new TransporterDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.InventoryItem = new InventoryItemDatabase(api, Mod.Info.ModID);
    }
}