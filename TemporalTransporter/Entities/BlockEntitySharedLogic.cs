using System;
using System.IO;
using TemporalTransporter.Database;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.Entities;

public static class BlockEntitySharedLogic
{
    public static void UpdateInventory(ICoreAPI api, IInventory inventory, Vec3i atPos)
    {
        var inventoryItems =
            DatabaseAccessor.InventoryItem.GetInventoryItems(DatabaseAccessor.GetCoordinateKey(atPos));

        foreach (var inventoryItem in inventoryItems)
        {
            if (inventoryItem.ItemBlob == null || inventoryItem.ItemBlob.Length == 0)
            {
                continue;
            }

            using var memoryStream = new MemoryStream(inventoryItem.ItemBlob);
            using var binaryReader = new BinaryReader(memoryStream);

            var itemstack = new ItemStack(binaryReader, api.World);
            var itemSlot = inventory[inventoryItem.SlotId + 2];
            itemSlot.Itemstack = itemstack;

            itemSlot.MarkDirty();
        }
    }

    public static byte[] ItemstackToBytes(ItemStack? itemStack)
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
}