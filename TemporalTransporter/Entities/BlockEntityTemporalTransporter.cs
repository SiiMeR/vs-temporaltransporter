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
    private WeatherSystemServer? _weatherSystem;
    public bool IsConnected; // TODO: figure out from database
    public bool IsCovered;

    public bool IsOnCooldown;

    public BlockEntityTemporalTransporter(InventoryGeneric inventory)
    {
        _inventory = inventory;
    }


    // TODO: all state should be loaded from database. since the things work without having them loaded, their state needs to be tracked externally
    public BlockEntityTemporalTransporter()
    {
        _inventory = new InventoryGeneric(10, null, null, (id, self) =>
        {
            if (id == InputSlotIndex)
            {
                return new ItemSlotLimited(self,
                    new[]
                    {
                        "envelope-*", "book-normal-*", "paper-parchment"
                    });
            }

            if (id == KeySlotIndex)
            {
                return new ItemSlotLimited(self, new[] { "transporterkey" });
            }

            // received mail slots are take only
            return new ItemSlotLimited(self, Array.Empty<string>());
        });
    }

    public int InputSlotIndex => 8;
    public ItemSlot InputSlot => _inventory[InputSlotIndex];

    public int KeySlotIndex => 9;
    public ItemSlot KeySlot => _inventory[KeySlotIndex];


    public string? KeyCode => KeySlot.Itemstack?.Attributes.GetString("keycode");

    public override InventoryBase Inventory => _inventory;
    public override string InventoryClassName { get; } = "temporaltransporterInv";
    public int ChargeCount { get; set; }

    public override void Initialize(ICoreAPI api)
    {
        _inventory.LateInitialize($"{InventoryClassName}-{Pos}", api);
        _inventory.SlotModified += OnItemSlotModified;
        base.Initialize(api);

        _weatherSystem = Api.ModLoader.GetModSystem<WeatherSystemServer>();


        api.Event.RegisterEventBusListener(OnChargeAdded, filterByEventName: Events.Charged);
        api.Event.RegisterEventBusListener(OnCoveredStateChanged, filterByEventName: Events.SetCoveredState);


        if (api.Side == EnumAppSide.Server && KeyCode != null)
        {
            IsConnected = DatabaseAccessor.Transporter.GetTransportersByConnectionKey(KeyCode).Length > 1;
            MarkDirty();
        }
    }

    private void OnCoveredStateChanged(string eventName, ref EnumHandling handling, IAttribute data)
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

        if (Api.Side == EnumAppSide.Server)
        {
            var wasCovered = IsCovered;

            IsCovered = DatabaseAccessor.Covered.GetIsCovered(Pos.ToVec3i());
            if (wasCovered != IsCovered)
            {
                MarkDirty();
            }
        }
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

        if (Api.Side == EnumAppSide.Server)
        {
            ChargeCount = DatabaseAccessor.Charge.IncrementCharge(pos, TemporalTransporterModSystem.Config?.ChargesPerGear ?? 1);
            MarkDirty();
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
        if (slotId < 8)
        {
            DatabaseAccessor.InventoryItem
                .UpdateInventoryItemSlot(DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i()),
                    slotId, BlockEntitySharedLogic.ItemstackToBytes(itemStack));

            return;
        }

        if (slotId == KeySlotIndex)
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

        foreach (var transporterId in ids)
        {
            var coords = DatabaseAccessor.CoordinateKeyToVec3i(transporterId);
            var blockPos = new BlockPos(coords.X, coords.Y, coords.Z);
            var blockEntity = Api.World.BlockAccessor.GetBlockEntity<BlockEntityTemporalTransporter>(blockPos);

            blockEntity?.OnConnectedServerSide();
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
            DatabaseAccessor.Charge.InitializeCharges(Pos.ToVec3i());
        }


        base.OnBlockPlaced(byItemStack);
    }


    public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
    {
        if (packetid == PacketIds.SendItemPacketId)
        {
            OnSendItem(player);
            return;
        }

        base.OnReceivedClientPacket(player, packetid, data);
    }

    public void OnConnectedClientSide()
    {
        _dialog?.TryClose();
    }

    public void OnConnectedServerSide()
    {
        IsConnected = true;
        MarkDirty();
    }

    public void OnSendItem(IPlayer byPlayer)
    {
        if (IsCovered)
        {
            return;
        }


        if (InputSlot.Itemstack == null)
        {
            return;
        }

        if (Api is not ICoreServerAPI serverApi)
        {
            return;
        }

        if (byPlayer is not IServerPlayer player)
        {
            return;
        }


        if (IsOnCooldown)
        {
            player.SendIngameError("ttrans-cooldown",
                Util.LangStr("error-temporaltransporter-oncooldown"));

            return;
        }


        var connectionKey = KeySlot.Itemstack?.Attributes?.GetString("keycode");
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
        var targetIsInterceptor = false;

        var interceptorRadius = TemporalTransporterModSystem.Config?.InterceptorRadius ?? 10;

        foreach (var interceptor in interceptors)
        {
            var interceptorPos = DatabaseAccessor.CoordinateKeyToVec3i(interceptor.CoordinateKey);

            if (BlockEntitySharedLogic.IsInterceptorCatchingBeam(senderPos, receiverPos, interceptorPos,
                    interceptorRadius) &&
                HasFreeSlot(interceptorPos) &&
                HasCharge(interceptorPos)
                && !InterceptorIsCovered(interceptorPos))
            {
                targetPosition = interceptor.CoordinateKey;
                targetIsInterceptor = true;
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

        if (InterceptorIsCovered(receiverPos))
        {
            player.SendIngameError("ttrans-covered",
                Util.LangStr("error-temporaltransporter-covered"));
            return;
        }


        var itemStack = InputSlot.TakeOut(1);
        MoveItemToPosition(itemStack, targetPosition, suitableSlot);
        _inventory.MarkSlotDirty(InputSlotIndex);


        IsOnCooldown = true;
        serverApi.Event.RegisterCallback(dt => { IsOnCooldown = false; },
            TemporalTransporterModSystem.Config?.SendCooldownSeconds * 1000 ?? 1000);


        // TODO: These 3 should be tied together into an atomic operation (perhaps having db be the sot)
        var newChargeCount = DatabaseAccessor.Charge.DecrementCharge(Pos.ToVec3i());
        ChargeCount = newChargeCount;
        MarkDirty();

        if (targetIsInterceptor)
        {
            _weatherSystem?.SpawnLightningFlash(DatabaseAccessor.CoordinateKeyToVec3d(targetPosition));
            DatabaseAccessor.Charge.DecrementCharge(DatabaseAccessor.CoordinateKeyToVec3i(targetPosition));
        }

        Api.World
            .PlaySoundAt(new AssetLocation("game:sounds/effect/translocate-breakdimension"),
                Pos.X, Pos.Y, Pos.Z, null, true, 64f);

        var targetPositionVec3d = DatabaseAccessor.CoordinateKeyToVec3d(targetPosition);

        Api.World
            .PlaySoundAt(new AssetLocation("game:sounds/effect/translocate-breakdimension"),
                targetPositionVec3d.X, targetPositionVec3d.Y, targetPositionVec3d.Z, null, true, 64f);
    }

    private bool InterceptorIsCovered(Vec3i interceptorPos)
    {
        return DatabaseAccessor.Covered.GetIsCovered(interceptorPos);
    }

    private bool HasCharge(Vec3i interceptorPos)
    {
        return DatabaseAccessor.Charge.GetChargeCount(interceptorPos) > 0;
    }

    private bool HasFreeSlot(Vec3i interceptorPos)
    {
        return DatabaseAccessor.InventoryItem.GetFirstFreeSlotId(DatabaseAccessor.GetCoordinateKey(interceptorPos)) !=
               -1;
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
                DatabaseAccessor.Covered.DeleteChargeTrackingForPosition(Pos.ToVec3i());
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
            BlockEntitySharedLogic.UpdateInventory(api, Inventory, Pos.ToVec3i());
            ChargeCount = DatabaseAccessor.Charge.GetChargeCount(Pos.ToVec3i());
            MarkDirty();


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

        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
        IsCovered = tree.GetBool("covered");

        IsConnected = tree.GetBool("connected");
        ChargeCount = tree.GetInt("chargeCount");

        base.FromTreeAttributes(tree, worldForResolving);

        _dialog?.Update();
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        Inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;
        tree.SetBool("covered", IsCovered);
        tree.SetBool("connected", IsConnected);
        tree.SetInt("chargeCount", ChargeCount);
    }
}