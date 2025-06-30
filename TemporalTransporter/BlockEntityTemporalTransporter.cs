using TemporalTransporter.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TemporalTransporter;

public class BlockEntityTemporalTransporter : BlockEntityOpenableContainer
{
    private InventoryGeneric _inventory;
    private GuiDialogTemporalTransporter? _openDialog;

    public BlockEntityTemporalTransporter(InventoryGeneric inventory)
    {
        _inventory = inventory;
    }

    public BlockEntityTemporalTransporter()
    {
    }


    public override InventoryBase Inventory => _inventory;
    public override string InventoryClassName { get; } = "temporaltransporterInv";

    public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
    {
        var api = byPlayer.Entity.World.Api;

        if (api.Side != EnumAppSide.Client || api is not ICoreClientAPI capi)
        {
            return false;
        }

        _openDialog ??= new GuiDialogTemporalTransporter(Inventory, blockSel.Position, capi);

        _openDialog?.TryOpen();

        return true;
    }

    public override void Initialize(ICoreAPI api)
    {
        _inventory = new InventoryGeneric(1, $"{InventoryClassName}-{Pos}", api);
        base.Initialize(api);
    }
}