using TemporalTransporter.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace TemporalTransporter;

public class BlockEntityTemporalTransporter : BlockEntityOpenableContainer
{
    private readonly InventoryGeneric _inventory;
    private GuiDialogTemporalTransporter? _dialog;

    public BlockEntityTemporalTransporter(InventoryGeneric inventory)
    {
        _inventory = inventory;
    }

    public BlockEntityTemporalTransporter()
    {
        _inventory = new InventoryGeneric(9, null, null);
    }


    public override InventoryBase Inventory => _inventory;
    public override string InventoryClassName { get; } = "temporaltransporterInv";

    public override void Initialize(ICoreAPI api)
    {
        _inventory.LateInitialize($"{InventoryClassName}-{Pos}", api);
        base.Initialize(api);
    }

    public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
    {
        var api = byPlayer.Entity.World.Api;

        if (api.Side != EnumAppSide.Client || api is not ICoreClientAPI capi)
        {
            return true;
        }

        toggleInventoryDialogClient(byPlayer, () =>
        {
            _dialog ??= new GuiDialogTemporalTransporter(Inventory, Pos, capi, this);
            // _dialog?.Update();

            return _dialog;
        });


        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

        base.FromTreeAttributes(tree, worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        Inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;
    }
}