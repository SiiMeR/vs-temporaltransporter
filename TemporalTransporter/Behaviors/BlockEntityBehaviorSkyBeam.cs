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
        // TODO use hsv
        var colorRgba = properties["beamColorRGBA"].AsString().Split(",").Select(c => Convert.ToInt32(c)).ToArray();
        _colorRgba = ColorUtil.ColorFromRgba(colorRgba[0], colorRgba[1], colorRgba[2], colorRgba[3]);

        _beam = new SimpleParticleProperties(0.4f, 0.7f, ColorUtil.ToRgba(50, 220, 220, 220), new Vec3d(),
            new Vec3d(), new Vec3f(-0.1f, -0.1f, -0.1f), new Vec3f(0.1f, 1.5f, 0.1f), 1.5f, 0.0f)
        {
            MinSize = 0.2f,
            MinPos = new Vec3d(Pos.X + 0.5, Pos.Y + 1, Pos.Z + 0.5),
            SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.6f),
            VertexFlags = 255,
            SelfPropelled = true,
            ParticleModel = EnumParticleModel.Quad,
            OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEAR, -150f)
        };


        if (api is { Side: EnumAppSide.Client })
        {
            _listenerId = api.Event.RegisterGameTickListener(OnGameTick, 50);
        }

        api.Event.RegisterEventBusListener(OnDisabledStateChanged, filterByEventName: Events.SetCoveredState);


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

        _isDisabled = tree.GetBool("isDisabled");
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
        if (Api.Side == EnumAppSide.Client)
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

        var h = 110 + Api.World.Rand.Next(15);
        var v = 100 + Api.World.Rand.Next(50);

        var color = Block.EntityClass == "TemporalTransporter"
            ? ColorUtil.HsvToRgba(h, 180, v, 150)
            : ColorUtil.HsvToRgba(h + 120, 180, v, 150);

        _beam.Color = ColorUtil.ReverseColorBytes(color);

        var multiplier = 0.8f;

        var minPosX = Random.Shared.NextDouble() * multiplier * 2 - multiplier;
        var minPosZ = Random.Shared.NextDouble() * multiplier * 2 - multiplier;
        _beam.AddPos.Set(minPosX, 1, minPosZ);

        Api.World.SpawnParticles(_beam);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        _isDisabled = tree.GetBool("isDisabled");
        base.FromTreeAttributes(tree, worldAccessForResolve);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetBool("isDisabled", _isDisabled);
    }
}