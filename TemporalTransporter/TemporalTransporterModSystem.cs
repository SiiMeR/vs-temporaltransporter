using System;
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
        api.RegisterBlockBehaviorClass("Chargeable", typeof(BlockBehaviorChargeable));

        api.Network.RegisterChannel(Mod.Info.ModID)
            .RegisterMessageType<TransportersConnectedPacket>()
            .RegisterMessageType<SyncChargesPacket>();
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;
        ClientNetworkChannel = api.Network.GetChannel(Mod.Info.ModID)
            .SetMessageHandler<TransportersConnectedPacket>(OnTransportersConnected)
            .SetMessageHandler<SyncChargesPacket>(OnSyncChargesPacketReceived);
    }

    private void OnSyncChargesPacketReceived(SyncChargesPacket packet)
    {
        if (ClientApi == null)
        {
            throw new InvalidOperationException("ClientApi is not initialized.");
        }

        var coords = DatabaseAccessor.CoordinateKeyToVec3i(packet.CoordinateKey);

        var blockPos = new BlockPos(coords.X, coords.Y, coords.Z);
        var blockEntity = ClientApi.World.BlockAccessor.GetBlockEntity(blockPos);

        if (blockEntity is BlockEntityTemporalInterceptor blockEntityInterceptor)
        {
            blockEntityInterceptor.UpdateChargeCount(packet.ChargeCount);
            blockEntityInterceptor.MarkDirty();
        }
        else if (blockEntity is BlockEntityTemporalTransporter blockEntityTransporter)
        {
            blockEntityTransporter.UpdateChargeCount(packet.ChargeCount);
            blockEntityTransporter.MarkDirty();
        }
    }

    private void OnTransportersConnected(TransportersConnectedPacket packet)
    {
        if (ClientApi == null)
        {
            throw new InvalidOperationException("ClientApi is not initialized.");
        }

        foreach (var transporterId in packet.TransporterIds)
        {
            var coords = DatabaseAccessor.CoordinateKeyToVec3i(transporterId);
            var blockPos = new BlockPos(coords.X, coords.Y, coords.Z);
            var blockEntity = ClientApi.World.BlockAccessor.GetBlockEntity<BlockEntityTemporalTransporter>(blockPos);

            blockEntity?.SetIsConnected(true);
        }
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        ServerApi = api;
        DatabaseAccessor.Transporter = new TransporterDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.Interceptor = new InterceptorDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.InventoryItem = new InventoryItemDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.Charge = new ChargeDatabase(api, Mod.Info.ModID);

        ServerNetworkChannel = api.Network.GetChannel(Mod.Info.ModID);
    }
}