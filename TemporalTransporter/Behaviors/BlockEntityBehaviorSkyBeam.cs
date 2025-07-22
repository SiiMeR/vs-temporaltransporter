using System;
using TemporalTransporter.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Behaviors;

public class BlockEntityBehaviorSkyBeam : BlockEntityBehavior
{
    private float _accum;

    private long _listenerId;


    public BlockEntityBehaviorSkyBeam(BlockEntity blockentity) : base(blockentity)
    {
    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        if (api is { Side: EnumAppSide.Client })
        {
            _listenerId = api.Event.RegisterGameTickListener(OnGameTick, 50);
        }

        base.Initialize(api, properties);
    }

    public override void OnBlockRemoved()
    {
        if (TemporalTransporterModSystem.ClientApi is { Side: EnumAppSide.Client })
        {
            TemporalTransporterModSystem.ClientApi.Event.EnqueueMainThreadTask(
                () => { TemporalTransporterModSystem.ClientApi.Event.UnregisterGameTickListener(_listenerId); },
                "UnregisterSkyBeamListener");
        }

        base.OnBlockRemoved();
    }


    public override void OnBlockUnloaded()
    {
        if (TemporalTransporterModSystem.ClientApi is { Side: EnumAppSide.Client })
        {
            TemporalTransporterModSystem.ClientApi.Event.UnregisterGameTickListener(_listenerId);
        }

        base.OnBlockUnloaded();
    }

    public void OnGameTick(float dt)
    {
        var rainmapHeight = Api.World.BlockAccessor.GetRainMapHeightAt(Pos);

        // something covering it
        if (rainmapHeight > Pos.Y)
        {
            return;
        }

        if (Blockentity is not BlockEntityTemporalTransporter temporalTransporter ||
            temporalTransporter.IsDisabled)
        {
            return;
        }

        var api = TemporalTransporterModSystem.ClientApi;

        if (api is not { Side: EnumAppSide.Client })
        {
            return;
        }

        var beam = new SimpleParticleProperties(
                2, 5,
                ColorUtil.ColorFromRgba(200, 155, 0, 220),
                new Vec3d(), new Vec3d(),
                new Vec3f(0, 0.5f, 0), new Vec3f(0, 1f, 0),
                1f, 0f, 0.08f, 0.18f,
                EnumParticleModel.Quad
            )
            { SelfPropelled = true, VertexFlags = 255 };

        _accum += dt;
        if (_accum < 0.15f)
        {
            return;
        }

        _accum = 0;


        var multiplier = 0.8f;

        var minPosX = Random.Shared.NextDouble() * multiplier * 2 - multiplier;
        var minPosZ = Random.Shared.NextDouble() * multiplier * 2 - multiplier;

        beam.MinPos.Set(Pos.X + 0.5, Pos.Y + 1.1, Pos.Z + 0.5);
        beam.AddPos.Set(minPosX, 3, minPosZ);

        Api.World.SpawnParticles(beam);
    }
}