using System;
using System.IO;
using System.Linq;
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

    public BlockEntityTemporalTransporter(InventoryGeneric inventory)
    {
        _inventory = inventory;
    }

    public BlockEntityTemporalTransporter()
    {
        _inventory = new InventoryGeneric(10, null, null, (id, self) =>
        {
            if (id == 0)
            {
                return new ItemSlotSurvival(self);
            }

            if (id == 1)
            {
                return new ItemSlotLimited(self, new[] { "temporaltransporter:transporterkey" });
            }

            return new ItemSlotLimited(self, Array.Empty<string>());
        });
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

        var itemStack = _inventory[slotId].Itemstack;


        if (slotId > 1)
        {
            DatabaseAccessor.InventoryItem
                .UpdateInventoryItemSlot(DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i()),
                    slotId - 2, ItemstackToBytes(itemStack));

            return;
        }

        if (slotId == 1)
        {
            if (itemStack == null)
            {
                DatabaseAccessor.Transporter.SetTransporterConnectionKey(Pos.ToVec3i(), string.Empty);
                return;
            }

            if (itemStack.Collectible is not ItemTransporterKey)
            {
                return;
            }

            var code = itemStack.Attributes.GetString("keycode");
            if (string.IsNullOrWhiteSpace(code))
            {
                return;
            }


            DatabaseAccessor.Transporter.SetTransporterConnectionKey(Pos.ToVec3i(), code);
        }
    }


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        if (Api.Side == EnumAppSide.Server)
        {
            DatabaseAccessor.Transporter.InsertTransporter(new Transporter
            {
                CoordinateKey = DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i())
            });

            DatabaseAccessor.InventoryItem.InitializeInventoryForPosition(Pos.ToVec3i());
        }


        base.OnBlockPlaced(byItemStack);
    }


    public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
    {
        if (packetid == 1337)
        {
            OnSendItem();
        }

        base.OnReceivedClientPacket(player, packetid, data);
    }

    public void OnSendItem()
    {
        if (Api.Side != EnumAppSide.Server || _inventory[0].Itemstack == null)
        {
            return;
        }

        var connectionKey = _inventory[1].Itemstack?.Attributes?.GetString("keycode");
        if (string.IsNullOrWhiteSpace(connectionKey))
        {
            return;
        }

        var itemStack = _inventory[0].TakeOut(1);
        if (itemStack is not { StackSize: > 0 })
        {
            return;
        }


        var toPosition = DatabaseAccessor.Transporter.GetTransportersByConnectionKey(
                connectionKey)
            ?.First(transporter => transporter.CoordinateKey != DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i()))
            ?.CoordinateKey;
        if (string.IsNullOrWhiteSpace(toPosition))
        {
            Api.World.Logger.Error($"Failed to find transporter with connection key {connectionKey}");
            return;
        }

        MoveItemToPosition(itemStack, toPosition);

        _inventory.MarkSlotDirty(0);
    }

    public void MoveItemToPosition(ItemStack itemStack, string toCoordinateKey)
    {
        try
        {
            var itemBytes = ItemstackToBytes(itemStack);


            DatabaseAccessor.InventoryItem.UpdateInventoryItemSlot(toCoordinateKey, 0, itemBytes);
        }
        catch (Exception e)
        {
            Api.World.Logger.Error($"Failed to move item {itemStack} from {Pos} to {toCoordinateKey}: {e.Message}");
        }
    }

    private static byte[] ItemstackToBytes(ItemStack? itemStack)
    {
        if (itemStack == null)
        {
            return Array.Empty<byte>();
        }

        MemoryStream? stream = null;
        try
        {
            stream = new MemoryStream();
            using var binaryWriter = new BinaryWriter(stream);
            itemStack.ToBytes(binaryWriter);
            return stream.ToArray();
        }
        catch
        {
            stream?.Dispose();
            throw;
        }
    }

    public override void OnBlockRemoved()
    {
        if (Api.Side == EnumAppSide.Server)
        {
            try
            {
                DatabaseAccessor.Transporter.RemoveTransporterByPosition(Pos.ToVec3i());
                DatabaseAccessor.InventoryItem.ClearInventoryForPosition(Pos.ToVec3i());
            }
            catch (Exception e)
            {
                Api.World.Logger.Warning(
                    $"Removed transporter at {Pos} but failed to remove from database: {e.Message}");
            }
        }

        base.OnBlockRemoved();
    }


    public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
    {
        var api = byPlayer.Entity.World.Api;

        if (api.Side == EnumAppSide.Server)
        {
            var inventory =
                DatabaseAccessor.InventoryItem.GetInventoryItems(DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i()));

            foreach (var inventoryItem in inventory)
            {
                if (inventoryItem.ItemBlob == null || inventoryItem.ItemBlob.Length == 0)
                {
                    continue;
                }

                using var memoryStream = new MemoryStream(inventoryItem.ItemBlob);
                using var binaryReader = new BinaryReader(memoryStream);

                var itemstack = new ItemStack(binaryReader, api.World);
                var itemSlot = _inventory[inventoryItem.SlotId + 2];
                itemSlot.Itemstack = itemstack;

                itemSlot.MarkDirty();
            }

            return true;
        }

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