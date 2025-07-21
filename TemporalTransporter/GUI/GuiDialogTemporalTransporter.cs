using System;
using TemporalTransporter.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.GUI;

public class GuiDialogTemporalTransporter : GuiDialogBlockEntity
{
    private readonly BlockEntityTemporalTransporter _blockEntity;
    private bool _isConnected;

    private bool _isDisabled;

    public GuiDialogTemporalTransporter(InventoryBase inventory, BlockPos bePos, ICoreClientAPI capi,
        BlockEntityTemporalTransporter blockEntity) :
        base("Temporal Transporter", inventory, bePos, capi)
    {
        _blockEntity = blockEntity;
        Inventory.SlotModified += OnItemSlotModified;

        // SetupDebugHandlers();
    }

    public override string ToggleKeyCombinationCode => null!;

    ~GuiDialogTemporalTransporter()
    {
        Inventory.SlotModified -= OnItemSlotModified;
    }

    public void SetupDebugHandlers()
    {
        capi.Input.RegisterHotKey(
            "redraw",
            "Redraw",
            GlKeys.N,
            HotkeyType.GUIOrOtherControls
        );
        capi.Input.SetHotKeyHandler("redraw", _ => SetupDialog());
        capi.Event.RegisterGameTickListener(dt => { SetupDialog(); }, 100);
    }

    private bool SetupDialog()
    {
        var windowBounds = ElementBounds.Fixed(0, 0, 200, 250);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(windowBounds);

        var dialogBounds = ElementStdBounds.AutosizedMainDialog;

        var inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 30, 1, 1);
        var keySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None,
            150, 30, 1, 1);

        var sendButtonBounds =
            ElementBounds.Fixed(60, 30, 80, GuiElementPassiveItemSlot.unscaledSlotSize);

        var top = inputSlotBounds.fixedHeight + inputSlotBounds.fixedY + 70;

        var receivedMailBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, top, 4, 1);

        top = receivedMailBounds.fixedHeight + receivedMailBounds.fixedY;

        var receivedMailBounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, top, 4, 1);


        SingleComposer = capi.Gui
            .CreateCompo("temporaltransportergui", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .AddItemSlotGrid(Inventory, SendInvPacket, 1, new[] { 0 }, inputSlotBounds, "inputslot")
            .AddIf(_isConnected)
            .AddPassiveItemSlot(keySlotBounds, Inventory, Inventory[1])
            .EndIf()
            .AddIf(!_isConnected)
            .AddItemSlotGrid(Inventory, SendInvPacket, 1, new[] { 1 }, keySlotBounds, "keyslot")
            .EndIf()
            .AddButton("Send", OnSendClick, sendButtonBounds, CairoFont.SmallButtonText(), EnumButtonStyle.Normal,
                "sendButton")
            .AddIf(_isDisabled)
            .AddStaticText("Disabled: Not visible from sky",
                CairoFont.WhiteSmallText().WithFontSize(14).WithColor(new[] { 1d, 0d, 0d, 1d }),
                ElementBounds.Fixed(0, 100, 210, 20))
            .EndIf()
            .AddStaticText("Received Mail", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 125, 200, 20),
                "receivedMailTitle")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 2, 3, 4, 5 }, receivedMailBounds,
                "receivedMailBounds")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 6, 7, 8, 9 }, receivedMailBounds2,
                "receivedMailBounds2")
            .EndChildElements()
            .Compose();


        return true;
    }

    private void OnItemSlotModified(int slotId)
    {
        if (!SingleComposer.Composed)
        {
            return;
        }

        if (slotId != 0)
        {
            return;
        }

        var itemStack = Inventory[slotId].Itemstack;

        SingleComposer.GetButton("sendButton").Enabled = itemStack != null;
    }


    private bool OnSendClick()
    {
        var packet = new Packet_Client
        {
            BlockEntityPacket = new Packet_BlockEntityPacket
            {
                X = BlockEntityPosition.X,
                Y = BlockEntityPosition.Y,
                Z = BlockEntityPosition.Z,
                Packetid = 1337,
                Data = Array.Empty<byte>()
            },
            Id = 1337
        };

        capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y,
            BlockEntityPosition.Z, packet);

        return true;
    }

    private void SendInvPacket(object packet)
    {
        capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
    }


    private void OnTitleBarClose()
    {
        TryClose();
    }


    public override void OnGuiOpened()
    {
        SetupDialog();

        base.OnGuiOpened();
    }

    public void SetIsDisabled(bool isDisabled)
    {
        _isDisabled = isDisabled;
    }

    public void SetIsConnected(bool isConnected)
    {
        _isConnected = isConnected;
        SetupDialog();
    }
}