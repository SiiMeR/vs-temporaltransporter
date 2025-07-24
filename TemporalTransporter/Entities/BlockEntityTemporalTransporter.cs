using System;
using System.Linq;
using System.Numerics;
using TemporalTransporter.Database;
using TemporalTransporter.GUI;
using TemporalTransporter.Helpers;
using TemporalTransporter.Items;
using TemporalTransporter.Messages;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace TemporalTransporter.Entities;

public class BlockEntityTemporalTransporter : BlockEntityOpenableContainer
{
    private readonly InventoryGeneric _inventory;
    private GuiDialogTemporalTransporter? _dialog;
    public bool IsConnected;
    public bool IsDisabled;

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
                return new ItemSlotLimited(self,
                    new[]
                    {
                        "envelope-*", "book-normal-*", "paper-parchment"
                    });
            }

            if (id == 1)
            {
                return new ItemSlotLimited(self, new[] { "transporterkey" });
            }

            // received mail slots are take only
            return new ItemSlotLimited(self, Array.Empty<string>());
        });
    }


    public override InventoryBase Inventory => _inventory;
    public override string InventoryClassName { get; } = "temporaltransporterInv";
    public int ChargeCount { get; set; }

    public override void Initialize(ICoreAPI api)
    {
        _inventory.LateInitialize($"{InventoryClassName}-{Pos}", api);
        _inventory.SlotModified += OnItemSlotModified;
        base.Initialize(api);

        api.Event.RegisterEventBusListener(OnChargeAdded, filterByEventName: Events.Charged);
    }

    private void OnChargeAdded(string eventName, ref EnumHandling handling, IAttribute data)
    {
        if (data is not ITreeAttribute tree)
        {
            return;
        }

        var pos = tree.GetVec3i("position");
        if (Pos.ToVec3i() != pos)
        {
            return;
        }

        ChargeCount += 1;

        if (Api.Side == EnumAppSide.Server)
        {
            DatabaseAccessor.Charge.IncrementCharge(pos);
        }

        _dialog?.UpdateChargeCount();
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
                    slotId - 2, BlockEntitySharedLogic.ItemstackToBytes(itemStack));

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

            var transportersWithKey =
                DatabaseAccessor.Transporter.GetTransportersByConnectionKey(code);

            if (transportersWithKey.Length > 1)
            {
                LockKeySlot(transportersWithKey);
            }
        }
    }

    private void LockKeySlot(Transporter[] transportersWithKey)
    {
        var ids = transportersWithKey.Select(t => t.CoordinateKey).ToArray();

        if (TemporalTransporterModSystem.ServerNetworkChannel == null)
        {
            throw new InvalidOperationException("ServerNetworkChannel is not initialized.");
        }

        var players = Api.World.AllOnlinePlayers.Where(p => Inventory.openedByPlayerGUIds.Contains(p.PlayerUID))
            .ToArray();

        TemporalTransporterModSystem.ServerNetworkChannel.BroadcastPacket(new TransportersConnectedPacket
        {
            TransporterIds = ids
        }, players as IServerPlayer[]);
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
            DatabaseAccessor.Charge.InitializeCharges(Pos.ToVec3i());
        }


        base.OnBlockPlaced(byItemStack);
    }

    public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
    {
        if (packetid == 1337)
        {
            OnSendItem(player);
            return;
        }

        base.OnReceivedClientPacket(player, packetid, data);
    }

    public void SetIsConnected(bool isConnected)
    {
        IsConnected = isConnected;
        _dialog?.SetIsConnected(isConnected);
    }

    public void OnSendItem(IPlayer byPlayer)
    {
        if (IsDisabled)
        {
            return;
        }

        if (Api.Side != EnumAppSide.Server || _inventory[0].Itemstack == null)
        {
            return;
        }

        if (byPlayer is not IServerPlayer player)
        {
            return;
        }

        var connectionKey = _inventory[1].Itemstack?.Attributes?.GetString("keycode");
        if (string.IsNullOrWhiteSpace(connectionKey))
        {
            return;
        }

        if (DatabaseAccessor.Charge.GetChargeCount(Pos.ToVec3i()) <= 0)
        {
            Api.Logger.Warning($"Tried to send item from transporter at {Pos} but no charges left. Shouldn't happen.");
            return;
        }

        var toPosition = DatabaseAccessor.Transporter.GetTransportersByConnectionKey(
                connectionKey)
            ?.FirstOrDefault(transporter =>
                transporter.CoordinateKey != DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i()))
            ?.CoordinateKey;
        if (string.IsNullOrWhiteSpace(toPosition))
        {
            Api.World.Logger.Error($"Failed to find transporter with connection key {connectionKey}");
            return;
        }

        var receiverSplit = DatabaseAccessor.CoordinateKeyToVec3i(toPosition);


        var senderPos = Pos.ToVec3i();
        var receiverPos = new Vec3i(receiverSplit[0], receiverSplit[1], receiverSplit[2]);

        var interceptors = DatabaseAccessor.Interceptor.GetAllInterceptors();

        // order interceptors by distance to the sender
        interceptors = interceptors.OrderBy(i =>
        {
            var interceptorPos = DatabaseAccessor.CoordinateKeyToVector3(i.CoordinateKey);
            return Vector3.DistanceSquared(new Vector3(senderPos.X, senderPos.Y, senderPos.Z), interceptorPos);
        }).ToArray();

        var targetPosition = toPosition;
        foreach (var interceptor in interceptors)
        {
            var interceptorPos = DatabaseAccessor.CoordinateKeyToVec3i(interceptor.CoordinateKey);

            if (IsInterceptorCatchingBeam(senderPos, receiverPos, interceptorPos, 10f) && HasFreeSlot(interceptorPos))
            {
                targetPosition = interceptor.CoordinateKey;
                break;
            }
        }


        var suitableSlot = DatabaseAccessor.InventoryItem.GetFirstFreeSlotId(targetPosition);
        if (suitableSlot == -1)
        {
            player.SendIngameError("ttrans-full",
                Util.LangStr("error-temporaltransporter-destinationfull"));
            return;
        }


        var itemStack = _inventory[0].TakeOut(1);
        MoveItemToPosition(itemStack, targetPosition, suitableSlot);
        _inventory.MarkSlotDirty(0);


        // TODO: These 3 should be tied together into an atomic operation (perhaps having db be the sot)
        DatabaseAccessor.Charge.DecrementCharge(Pos.ToVec3i());
        ChargeCount -= 1;

        Api.World
            .PlaySoundAt(new AssetLocation("game:sounds/effect/translocate-breakdimension"),
                Pos.X, Pos.Y, Pos.Z,
                null, true, 16f);

        var weatherSys = Api.ModLoader.GetModSystem<WeatherSystemServer>();
        weatherSys.SpawnLightningFlash(DatabaseAccessor.CoordinateKeyToVec3d(targetPosition));
    }

    private bool HasFreeSlot(Vec3i interceptorPos)
    {
        return DatabaseAccessor.InventoryItem.GetFirstFreeSlotId(DatabaseAccessor.GetCoordinateKey(interceptorPos)) !=
               -1;
    }

    public static bool IsInterceptorCatchingBeam(Vec3i senderPos, Vec3i receiverPos, Vec3i interceptorPos, float radius)
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

    public void MoveItemToPosition(ItemStack itemStack, string toCoordinateKey, int slotId)
    {
        try
        {
            var itemBytes = BlockEntitySharedLogic.ItemstackToBytes(itemStack);
            DatabaseAccessor.InventoryItem.UpdateInventoryItemSlot(toCoordinateKey, slotId, itemBytes);


            // TODO: this below is a side effect that should get trigerred via message

            var entityAtTarget =
                Api.World.BlockAccessor.GetBlockEntity(DatabaseAccessor.CoordinateKeyToVec3i(toCoordinateKey)
                    .ToBlockPos());

            if (entityAtTarget is BlockEntityTemporalTransporter temporalTransporter)
            {
                BlockEntitySharedLogic.UpdateInventory(Api, temporalTransporter.Inventory,
                    temporalTransporter.Pos.ToVec3i());
            }
        }
        catch (Exception e)
        {
            Api.World.Logger.Error($"Failed to move item {itemStack} from {Pos} to {toCoordinateKey}: {e.Message}");
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
                DatabaseAccessor.Charge.DeleteChargeTrackingForPosition(Pos.ToVec3i());
            }
            catch (Exception e)
            {
                Api.World.Logger.Warning(
                    $"Removed transporter at {Pos} but failed to remove from database: {e.Message}");
            }
        }

        Inventory[1].TakeOut(1);
        Inventory.MarkSlotDirty(1);

        base.OnBlockRemoved();
    }

    public override void OnBlockBroken(IPlayer byPlayer = null)
    {
        // Inventory[1].TakeOut(1);
        // Inventory.MarkSlotDirty(1);

        base.OnBlockBroken(byPlayer);
    }

    public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
    {
        var api = byPlayer.Entity.World.Api;

        if (api.Side == EnumAppSide.Server)
        {
            BlockEntitySharedLogic.UpdateInventory(api, Inventory, Pos.ToVec3i());

            return true;
        }

        if (api.Side != EnumAppSide.Client || api is not ICoreClientAPI capi)
        {
            return true;
        }


        // used for refueling, is it even needed if blockbehaviors are before this?
        if (byPlayer.Entity.Controls.CtrlKey)
        {
            return true;
        }

        toggleInventoryDialogClient(byPlayer, () =>
        {
            _dialog ??= new GuiDialogTemporalTransporter(Inventory, Pos, capi, this);

            return _dialog;
        });

        _dialog?.SetIsConnected(IsConnected);

        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
        IsDisabled = tree.GetBool("disabled");
        IsConnected = tree.GetBool("connected");
        ChargeCount = tree.GetInt("chargeCount");

        base.FromTreeAttributes(tree, worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        Inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;
        tree.SetBool("disabled", IsDisabled);
        tree.SetBool("connected", IsConnected);
        tree.SetInt("chargeCount", ChargeCount);
    }

    public void Disable()
    {
        if (IsDisabled)
        {
            return;
        }

        IsDisabled = true;
        _dialog?.Redraw();
    }

    public void Enable()
    {
        if (!IsDisabled)
        {
            return;
        }

        IsDisabled = false;
        _dialog?.Redraw();
    }
}