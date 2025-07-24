using System;
using TemporalTransporter.Database;
using TemporalTransporter.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
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
            return new ItemSlotLimited(self, Array.Empty<string>());
        });
    }

    public int ChargeCount { get; set; }

    private BlockEntityAnimationUtil? AnimUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;


    public override InventoryBase Inventory => _inventory;
    public override string InventoryClassName { get; } = "temporalinterceptorInv";

    public override void Initialize(ICoreAPI api)
    {
        _inventory.LateInitialize($"{InventoryClassName}-{Pos}", api);
        _inventory.SlotModified += OnItemSlotModified;
        base.Initialize(api);

        if (api.World.Side == EnumAppSide.Client)
        {
            AnimUtil?.InitializeAnimator("interceptor", null, null, new Vec3f(0, Block.Shape.rotateY, 0));
        }

        api.Event.RegisterEventBusListener(OnChargeAdded, filterByEventName: Events.Charged);
        api.Event.RegisterEventBusListener(OnDisabledStateChanged, filterByEventName: Events.SetDisabledState);
    }

    private void OnDisabledStateChanged(string eventName, ref EnumHandling handling, IAttribute data)
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

        var isDisabled = tree.GetBool("isDisabled");
        if (isDisabled)
        {
            Disable();
        }
        else
        {
            Enable();
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

        DatabaseAccessor.InventoryItem
            .UpdateInventoryItemSlot(DatabaseAccessor.GetCoordinateKey(Pos.ToVec3i()),
                slotId, BlockEntitySharedLogic.ItemstackToBytes(itemStack));
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
            DatabaseAccessor.Charge.InitializeCharges(Pos.ToVec3i());
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
                DatabaseAccessor.Charge.DeleteChargeTrackingForPosition(Pos.ToVec3i());
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

        AnimUtil?.StartAnimation(new AnimationMetaData
        {
            Animation = "active",
            Code = "active",
            AnimationSpeed = 1.0f
        });

        if (api.Side == EnumAppSide.Server)
        {
            BlockEntitySharedLogic.UpdateInventory(api, Inventory, Pos.ToVec3i());

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