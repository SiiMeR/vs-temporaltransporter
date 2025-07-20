using TemporalTransporter.Database;
using TemporalTransporter.GUI;
using TemporalTransporter.Items;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace TemporalTransporter;

public class BlockEntityTemporalTransporter : BlockEntityOpenableContainer
{
    private readonly InventoryGeneric _inventory;
    private GuiDialogTemporalTransporter? _dialog;
    private long _transporterId;

    public BlockEntityTemporalTransporter(InventoryGeneric inventory)
    {
        _inventory = inventory;
    }

    public BlockEntityTemporalTransporter()
    {
        _inventory = new InventoryGeneric(10, null, null);
    }


    public override InventoryBase Inventory => _inventory;
    public override string InventoryClassName { get; } = "temporaltransporterInv";

    public override void Initialize(ICoreAPI api)
    {
        _inventory.LateInitialize($"{InventoryClassName}-{Pos}", api);
        _inventory.SlotModified += OnItemSlotModified;
        base.Initialize(api);
    }

    private void OnItemSlotModified(int slotId)
    {
        if (Api.Side != EnumAppSide.Server)
        {
            return;
        }

        if (slotId != 1)
        {
            return;
        }

        var itemStack = _inventory[slotId].Itemstack;

        if (itemStack.Collectible is not ItemTransporterKey transporterKey)
        {
            return;
        }

        var code = itemStack.Attributes.GetString("keycode");
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        // TODO check if slot is emptied or added to

        DatabaseAccessor.Transporter.SetTransporterConnectionKey(_transporterId, code);
    }


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        if (Api.Side == EnumAppSide.Server)
        {
            var id = DatabaseAccessor.Transporter.InsertTransporter(new Transporter
            {
                X = Pos.X,
                Y = Pos.Y,
                Z = Pos.Z
            });

            _transporterId = id;
        }


        base.OnBlockPlaced(byItemStack);
    }

    public override void OnBlockRemoved()
    {
        if (Api.Side == EnumAppSide.Server)
        {
            DatabaseAccessor.Transporter.RemoveTransporterByPosition(Pos.X, Pos.Y, Pos.Z);
        }

        base.OnBlockRemoved();
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

            return _dialog;
        });


        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

        _transporterId = tree.GetLong("transporterId");

        base.FromTreeAttributes(tree, worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        Inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;
        tree.SetLong("transporterId", _transporterId);
    }
}