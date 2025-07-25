﻿using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TemporalTransporter.Behaviors;

public class BlockEntityBehaviorUncoverable : BlockEntityBehavior
{
    private long _listernerId;

    public BlockEntityBehaviorUncoverable(BlockEntity blockentity) : base(blockentity)
    {
    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        _listernerId = api.Event.RegisterGameTickListener(EveryXSeconds, 1000);
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();

        Api.Event.UnregisterGameTickListener(_listernerId);
    }

    private void EveryXSeconds(float _)
    {
        var rainmapHeight = Api.World.BlockAccessor.GetRainMapHeightAt(Pos);

        var isDisabled = rainmapHeight > Pos.Y;

        var tree = new TreeAttribute();
        tree.SetBool("isDisabled", isDisabled);
        tree.SetVec3i("position", Pos.ToVec3i());

        Api.Event.PushEvent(Events.SetDisabledState);
    }
}