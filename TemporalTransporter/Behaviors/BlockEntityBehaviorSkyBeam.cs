using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Behaviors;

public class BlockEntityBehaviorSkyBeam : BlockEntityBehavior
{
    private float _accum;

    private SimpleParticleProperties _beam;

    private int _colorRgba = ColorUtil.ColorFromRgba(0, 0, 0, 255);

    private bool _isDisabled;
    private long _listenerId;

    public BlockEntityBehaviorSkyBeam(BlockEntity blockentity) : base(blockentity)
    {
    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        var colorRgba = properties["beamColorRGBA"].AsString().Split(",").Select(c => Convert.ToInt32(c)).ToArray();
        _colorRgba = ColorUtil.ColorFromRgba(colorRgba[0], colorRgba[1], colorRgba[2], colorRgba[3]);

        _beam = new SimpleParticleProperties(
                2, 5,
                _colorRgba,
                new Vec3d(), new Vec3d(),
                new Vec3f(0, 0.4f, 0), new Vec3f(0, 0.9f, 0),
                1f, 0f, 0.08f, 0.18f,
                EnumParticleModel.Quad
            )
            { SelfPropelled = true, VertexFlags = 255 };


        if (api is { Side: EnumAppSide.Client })
        {
            _listenerId = api.Event.RegisterGameTickListener(OnGameTick, 50);
        }

        api.Event.RegisterEventBusListener(OnDisabledStateChanged, filterByEventName: Events.SetDisabledState);


        base.Initialize(api, properties);
    }

    private void OnDisabledStateChanged(string eventName, ref EnumHandling handling, IAttribute data)
    {
        if (data is not ITreeAttribute tree)
        {
            return;
        }

        var pos = tree.GetVec3i("position");
        if (Pos.ToVec3i() != pos)
        {
            return;
        }

        var isDisabled = tree.GetBool("isDisabled");
        if (isDisabled)
        {
            _isDisabled = true;
        }
        else
        {
            _isDisabled = false;
        }
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
        if (_isDisabled)
        {
            return;
        }


        _accum += dt;
        if (_accum < 0.15f)
        {
            return;
        }

        _accum = 0;


        var multiplier = 0.8f;

        var minPosX = Random.Shared.NextDouble() * multiplier * 2 - multiplier;
        var minPosZ = Random.Shared.NextDouble() * multiplier * 2 - multiplier;

        _beam.MinPos.Set(Pos.X + 0.5, Pos.Y + 0.6, Pos.Z + 0.5);
        _beam.AddPos.Set(minPosX, 2, minPosZ);

        Api.World.SpawnParticles(_beam);
    }
}