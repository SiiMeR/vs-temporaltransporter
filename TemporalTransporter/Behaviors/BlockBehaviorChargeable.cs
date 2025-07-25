using TemporalTransporter.Helpers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace TemporalTransporter.Behaviors;

public class BlockBehaviorChargeable : BlockBehavior
{
    public BlockBehaviorChargeable(Block block) : base(block)
    {
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel,
        ref EnumHandling handling)
    {
        if (!byPlayer.Entity.Controls.CtrlKey)
        {
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        var blockEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
        if (blockEntity == null)
        {
            world.Api.Logger.Error("Tried to charge a block that has no block entity at {0}", blockSel.Position);
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible is not ItemRustyGear)
        {
            return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
        }

        byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);

        var data = new TreeAttribute();
        data.SetVec3i("position", blockSel.Position.ToVec3i());

        world.Api.Event.PushEvent(Events.Charged, data);

        if (world.Api is ICoreClientAPI clientApi)
        {
            clientApi.ShowChatMessage(Util.LangStr("charged-success"));
            clientApi.Gui
                .PlaySound(new AssetLocation("game:sounds/tutorialstepsuccess"), true);
        }

        handling = EnumHandling.PreventSubsequent;
        return true;
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection,
        IPlayer forPlayer,
        ref EnumHandling handling)
    {
        return new[]
        {
            new WorldInteraction
            {
                ActionLangCode = "temporaltransporter:blockhelp-chargeable",
                Itemstacks = new[] { new ItemStack(world.GetItem(new AssetLocation("game:gear-rusty"))) },
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = "ctrl"
            }
        };
    }
}