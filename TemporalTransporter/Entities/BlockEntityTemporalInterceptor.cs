using System;
using System.IO;
using TemporalTransporter.Database;
using TemporalTransporter.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace TemporalTransporter.Entities;

public class BlockEntityTemporalInterceptor : BlockEntityOpenableContainer
{
    private readonly InventoryGeneric _inventory;
    private GuiDialogTemporalInterceptor? _dialog;

    public BlockEntityTemporalInterceptor(InventoryGeneric inventory)
    {
        _inventory = inventory;
    }

    public BlockEntityTemporalInterceptor()
    {
        _inventory = new InventoryGeneric(8, null, null, (id, self) =>
        {
            // received mail slots are take only
            return new ItemSlotLimited(self, Array.Empty<string>(), true);
        });
    }


    public override InventoryBase Inventory => _inventory;
    public override string InventoryClassName { get; } = "temporalinterceptorInv";

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

        DatabaseAccessor.InventoryItem
            .UpdateInventoryItemSlot(DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i()),
                slotId, ItemstackToBytes(itemStack));
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


    public override void OnBlockPlaced(ItemStack? byItemStack = null)
    {
        if (Api.Side == EnumAppSide.Server)
        {
            DatabaseAccessor.Interceptor.InsertInterceptor(new Interceptor
            {
                CoordinateKey = DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i())
            });

            DatabaseAccessor.InventoryItem.InitializeInventoryForPosition(Pos.ToVec3i());
        }


        base.OnBlockPlaced(byItemStack);
    }

    public override void OnBlockRemoved()
    {
        if (Api.Side == EnumAppSide.Server)
        {
            try
            {
                DatabaseAccessor.Interceptor.RemoveInterceptorByPosition(Pos.ToVec3i());
                DatabaseAccessor.InventoryItem.ClearInventoryForPosition(Pos.ToVec3i());
            }
            catch (Exception e)
            {
                Api.World.Logger.Warning(
                    $"Removed interceptor at {Pos} but failed to remove from database: {e.Message}");
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
                var itemSlot = _inventory[inventoryItem.SlotId];
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
            _dialog ??= new GuiDialogTemporalInterceptor(Inventory, Pos, capi, this);
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

    public void Disable()
    {
    }

    public void Enable()
    {
    }
}