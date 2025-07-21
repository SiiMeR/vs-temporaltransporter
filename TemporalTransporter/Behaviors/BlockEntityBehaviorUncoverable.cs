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

        if (api.World.Side == EnumAppSide.Server)
        {
            _listernerId = api.Event.RegisterGameTickListener(Every5Seconds, 5000);
        }
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();

        if (Api.World.Side == EnumAppSide.Server)
        {
            Api.Event.UnregisterGameTickListener(_listernerId);
        }
    }

    private void Every5Seconds(float _)
    {
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