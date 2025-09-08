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
    private static ICoreServerAPI? _serverApi;

    public static IServerNetworkChannel? ServerNetworkChannel;
    public static IClientNetworkChannel? ClientNetworkChannel;

    public static TemporalTransporterConfig? Config;
    private static ICoreClientAPI? _clientApi;

    public static ICoreClientAPI ClientApi
    {
        get => _clientApi ?? throw new NullReferenceException("ClientApi has not been initialized");
        set => _clientApi = value;
    }

    public static ICoreServerAPI ServerApi
    {
        get => _serverApi ?? throw new NullReferenceException("ServerApi has not been initialized");
        set => _serverApi = value;
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemTransporterKey", typeof(ItemTransporterKey));
        api.RegisterBlockEntityClass("TemporalTransporter", typeof(BlockEntityTemporalTransporter));
        api.RegisterBlockEntityClass("TemporalInterceptor", typeof(BlockEntityTemporalInterceptor));
        api.RegisterBlockEntityBehaviorClass("Uncoverable", typeof(BlockEntityBehaviorUncoverable));
        api.RegisterBlockEntityBehaviorClass("Skybeam", typeof(BlockEntityBehaviorSkyBeam));
        api.RegisterBlockBehaviorClass("Chargeable", typeof(BlockBehaviorChargeable));

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
            var coords = DatabaseAccessor.CoordinateKeyToVec3i(transporterId);
            var blockPos = new BlockPos(coords.X, coords.Y, coords.Z);
            var blockEntity = ClientApi.World.BlockAccessor.GetBlockEntity<BlockEntityTemporalTransporter>(blockPos);

            blockEntity?.SetIsConnected(true);
        }
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        ServerApi = api;

        Config = LoadConfig<TemporalTransporterConfig>(api);

        DatabaseAccessor.Transporter = new TransporterDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.Interceptor = new InterceptorDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.InventoryItem = new InventoryItemDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.Charge = new ChargeDatabase(api, Mod.Info.ModID);
        DatabaseAccessor.Covered = new CoveredDatabase(api, Mod.Info.ModID);

        ServerNetworkChannel = api.Network.GetChannel(Mod.Info.ModID);
    }

    public TConfig LoadConfig<TConfig>(ICoreServerAPI api, string? configName = null) where TConfig : new()
    {
        configName ??= $"{Mod.Info.ModID}.config.json";

        var config = api.LoadModConfig<TConfig>(configName);
        if (config == null)
        {
            config = new TConfig();
            api.StoreModConfig(config, configName);
        }

        // in case of new fields
        api.StoreModConfig(config, configName);

        return config;
    }
}