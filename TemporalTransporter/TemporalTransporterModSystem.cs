using System;
using System.Linq;
using TemporalTransporter.Behaviors;
using TemporalTransporter.Database;
using TemporalTransporter.Entities;
using TemporalTransporter.Items;
using TemporalTransporter.Messages;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TemporalTransporter;

public class TemporalTransporterModSystem : ModSystem
{
    public static ICoreServerAPI? ServerApi;
    public static ICoreClientAPI? ClientApi;

    public static IServerNetworkChannel? ServerNetworkChannel;
    public static IClientNetworkChannel? ClientNetworkChannel;

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemTransporterKey", typeof(ItemTransporterKey));
        api.RegisterBlockEntityClass("TemporalTransporter", typeof(BlockEntityTemporalTransporter));
        api.RegisterBlockEntityClass("TemporalInterceptor", typeof(BlockEntityTemporalInterceptor));
        api.RegisterBlockEntityBehaviorClass("Uncoverable", typeof(BlockEntityBehaviorUncoverable));
        api.RegisterBlockEntityBehaviorClass("Skybeam", typeof(BlockEntityBehaviorSkyBeam));

        api.Network.RegisterChannel(Mod.Info.ModID)
            .RegisterMessageType<TransportersConnectedPacket>();
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;
        ClientNetworkChannel = api.Network.GetChannel(Mod.Info.ModID)
            .SetMessageHandler<TransportersConnectedPacket>(OnTransportersConnected);
    }

    private void OnTransportersConnected(TransportersConnectedPacket packet)
    {
        if (ClientApi == null)
        {
            throw new InvalidOperationException("ClientApi is not initialized.");
        }

        foreach (var transporterId in packet.TransporterIds)
        {
            var coords = transporterId.Split(':').Select(t => Convert.ToInt32(t)).ToArray();
            var blockPos = new BlockPos(coords[0], coords[1], coords[2]);
            var blockEntity = ClientApi.World.BlockAccessor.GetBlockEntity<BlockEntityTemporalTransporter>(blockPos);

            blockEntity.SetIsConnected(true);
        }
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        ServerApi = api;
        DatabaseAccessor.Transporter = new TransporterDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.Interceptor = new InterceptorDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.InventoryItem = new InventoryItemDatabase(api, Mod.Info.ModID);

        ServerNetworkChannel = api.Network.GetChannel(Mod.Info.ModID);
    }
}