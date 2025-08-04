using System;
using TemporalTransporter.Entities;
using TemporalTransporter.Helpers;
using TemporalTransporter.Messages;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.GUI;

public class GuiDialogTemporalTransporter : GuiDialogBlockEntity
{
    private readonly BlockEntityTemporalTransporter _blockEntity;
    private bool _isConnected;

    public GuiDialogTemporalTransporter(InventoryBase inventory, BlockPos bePos, ICoreClientAPI capi,
        BlockEntityTemporalTransporter blockEntity) :
        base(Util.LangStr("block-temporaltransporter"), inventory, bePos, capi)
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

        var inputTextBounds = ElementBounds.Fixed(2, 21, 50, 20);
        var inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 40, 1, 1);

        var keyslotTextBounds = ElementBounds.Fixed(152, 21, 50, 20);

        var keySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None,
            150, 40, 1, 1);

        var sendButtonBounds =
            ElementBounds.Fixed(60, 40, 80, GuiElementPassiveItemSlot.unscaledSlotSize);

        var top = inputSlotBounds.fixedHeight + inputSlotBounds.fixedY + 70;

        var receivedMailBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, top, 4, 1);

        top = receivedMailBounds.fixedHeight + receivedMailBounds.fixedY;

        var receivedMailBounds2 = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, top, 4, 1);

        var chargesTextBounds = ElementBounds.Fixed(2, 95, 80, 20);
        var chargeCountBounds = chargesTextBounds.RightCopy(-10);
        SingleComposer = capi.Gui
            .CreateCompo("temporaltransportergui", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .AddStaticText(Util.LangStr("temporaltransporter-input-text"),
                CairoFont.WhiteSmallText().WithFontSize(14), inputTextBounds, "inputText")
            .AddItemSlotGrid(Inventory, SendInvPacket, 1, new[] { _blockEntity.InputSlotIndex }, inputSlotBounds,
                "inputslot")
            .AddStaticText(Util.LangStr("temporaltransporter-key-text"),
                CairoFont.WhiteSmallText().WithFontSize(14), keyslotTextBounds, "keyslotText")
            .AddIf(_isConnected)
            .AddPassiveItemSlot(keySlotBounds, Inventory, _blockEntity.KeySlot)
            .EndIf()
            .AddIf(!_isConnected)
            .AddItemSlotGrid(Inventory, SendInvPacket, 1, new[] { _blockEntity.KeySlotIndex }, keySlotBounds, "keyslot")
            .EndIf()
            .AddButton("Send", OnSendClick, sendButtonBounds, CairoFont.SmallButtonText(), EnumButtonStyle.Normal,
                "sendButton")
            .AddIf(_blockEntity.IsDisabled)
            .AddStaticText("Disabled: Not visible from sky",
                CairoFont.WhiteSmallText().WithFontSize(14).WithColor(new[] { 1d, 0d, 0d, 1d }),
                ElementBounds.Fixed(2, 115, 200, 20))
            .EndIf()
            .AddStaticText($"{Util.LangStr("charges-text")}:", CairoFont.WhiteSmallText().WithFontSize(14),
                chargesTextBounds,
                "chargesText")
            .AddDynamicText(_blockEntity.ChargeCount.ToString(), CairoFont.WhiteSmallText().WithFontSize(14),
                chargeCountBounds, "chargeCount")
            .AddStaticText(Util.LangStr("temporaltransporter-output-text"), CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(2, 140, 200, 20),
                "receivedMailTitle")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 0, 1, 2, 3 }, receivedMailBounds,
                "receivedMailBounds")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 4, 5, 6, 7 }, receivedMailBounds2,
                "receivedMailBounds2")
            .EndChildElements()
            .Compose();

        UpdateSendButtonState();

        return true;
    }


    public void UpdateChargeCount()
    {
        if (!SingleComposer.Composed)
        {
            return;
        }

        var chargeCount = Math.Max(_blockEntity.ChargeCount, 0);

        SingleComposer.GetDynamicText("chargeCount").SetNewText(chargeCount.ToString());
        UpdateSendButtonState();
    }

    public void UpdateSendButtonState()
    {
        SingleComposer.GetButton("sendButton").Enabled =
            _blockEntity is { ChargeCount: > 0, KeySlot.Empty: false, InputSlot.Empty: false, IsDisabled: false };
    }

    private void OnItemSlotModified(int slotId)
    {
        if (!SingleComposer.Composed)
        {
            return;
        }

        if (slotId != _blockEntity.InputSlotIndex)
        {
            return;
        }

        var itemStack = Inventory[slotId].Itemstack;

        SingleComposer.GetButton("sendButton").Enabled = itemStack != null && _blockEntity.ChargeCount > 0;
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
                Packetid = PacketIds.SendItemPacketId,
                Data = Array.Empty<byte>()
            },
            Id = PacketIds.SendItemPacketId
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

    public void SetIsConnected(bool isConnected)
    {
        _isConnected = isConnected;
        Redraw();
    }

    public void Redraw()
    {
        TryClose();
        SetupDialog();
        TryOpen();
    }
}