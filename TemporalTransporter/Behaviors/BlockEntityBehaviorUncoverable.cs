using TemporalTransporter.Entities;
using Vintagestory.API.Common;
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
        // TODO: interceptor blocking, message bus messaging
        if (Blockentity is not BlockEntityTemporalTransporter transporter)
        {
            return;
        }

        var rainmapHeight = Api.World.BlockAccessor.GetRainMapHeightAt(Pos);

        // something covering it
        if (rainmapHeight > Pos.Y)
        {
            transporter.Disable();
        }
        else
        {
            transporter.Enable();
        }
    }
}