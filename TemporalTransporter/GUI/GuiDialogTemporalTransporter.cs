using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.GUI;

public class GuiDialogTemporalTransporter : GuiDialogBlockEntity
{
    private BlockEntityTemporalTransporter _blockEntity;

    public GuiDialogTemporalTransporter(InventoryBase inventory, BlockPos bePos, ICoreClientAPI capi,
        BlockEntityTemporalTransporter blockEntity) :
        base("Temporal Transporter", inventory, bePos, capi)
    {
        _blockEntity = blockEntity;

        SetupDebugHandlers();
    }

    public override string ToggleKeyCombinationCode => null!;

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
            .AddItemSlotGrid(Inventory, SendInvPacket, 1, new[] { 1 }, keySlotBounds, "keyslot")
            .AddButton("Send", OnSendClick, sendButtonBounds, CairoFont.SmallButtonText(), EnumButtonStyle.Normal,
                "sendButton")
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

    private bool OnSendClick()
    {
        Console.WriteLine("Send clicked");

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
        base.OnGuiOpened();

        SetupDialog();
    }
}