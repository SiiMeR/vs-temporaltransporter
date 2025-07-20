using TemporalTransporter.Database;
using TemporalTransporter.Items;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace TemporalTransporter;

public class TemporalTransporterModSystem : ModSystem
{
    public static ICoreServerAPI? ServerApi;

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemTransporterKey", typeof(ItemTransporterKey));
        api.RegisterBlockEntityClass("TemporalTransporter", typeof(BlockEntityTemporalTransporter));
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        ServerApi = api;
        DatabaseAccessor.Transporter = new TransporterDatabase(api, Mod.Info.ModID);
    }
}