using System;
using System.IO;
using System.Numerics;
using TemporalTransporter.Database;
using TemporalTransporter.Messages;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

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
            var itemSlot = inventory[inventoryItem.SlotId];
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

    public static bool IsInterceptorCatchingBeam(Vec3i senderPos, Vec3i receiverPos, Vec3i interceptorPos, int radius)
    {
        var sender = new Vector2(senderPos.X, senderPos.Z);
        var receiver = new Vector2(receiverPos.X, receiverPos.Z);
        var interceptor = new Vector2(interceptorPos.X, interceptorPos.Z);

        var senderToReceiver = receiver - sender;
        var lenSquared = senderToReceiver.LengthSquared();

        float projection = 0;
        if (lenSquared != 0)
        {
            projection = Vector2.Dot(interceptor - sender, senderToReceiver) / lenSquared;
        }

        // Clamp t between 0 and 1
        projection = Math.Clamp(projection, 0, 1);

        var closestPoint = sender + projection * senderToReceiver;

        var distance = Vector2.Distance(interceptor, closestPoint);

        return distance <= radius;
    }

    public static void SyncCharges(Vec3i coords, IPlayer forPlayer)
    {
        var serverNetworkChannel = TemporalTransporterModSystem.ServerNetworkChannel;
        if (serverNetworkChannel == null)
        {
            throw new InvalidOperationException(
                "ServerNetworkChannel is not initialized. This method can only be called on the server side.");
        }

        var charges = DatabaseAccessor.Charge.GetChargeCount(coords);
        serverNetworkChannel.SendPacket(
            new SyncChargesPacket { CoordinateKey = DatabaseAccessor.GetCoordinateKey(coords), ChargeCount = charges },
            forPlayer as IServerPlayer);
    }
}