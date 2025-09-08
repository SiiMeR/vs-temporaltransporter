using System;
using TemporalTransporter.Entities;
using TemporalTransporter.Helpers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.GUI;

public class GuiDialogTemporalInterceptor : GuiDialogBlockEntity
{
    private readonly BlockEntityTemporalInterceptor _blockEntity;
    public bool IsDisabled;

    public GuiDialogTemporalInterceptor(InventoryBase inventory, BlockPos bePos, ICoreClientAPI capi,
        BlockEntityTemporalInterceptor blockEntity) :
        base("Temporal Interceptor", inventory, bePos, capi)
    {
        _blockEntity = blockEntity;
        // SetupDebugHandlers();
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
        var windowBounds = ElementBounds.Fixed(0, 0, 200, 150);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(windowBounds);

        var dialogBounds = ElementStdBounds.AutosizedMainDialog;

        var receivedMailBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 100, 4, 1);
        var receivedMailBounds2 =
            ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, receivedMailBounds.fixedY + 50, 4, 1);

        var chargesTextBounds = ElementBounds.Fixed(2, 20, 80, 20);
        var chargeCountBounds = chargesTextBounds.RightCopy(-10);

        SingleComposer = capi.Gui
            .CreateCompo("temporalinterceptorgui", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .AddIf(_blockEntity.IsCovered)
            .AddStaticText("Disabled: Not visible from sky",
                CairoFont.WhiteSmallText().WithFontSize(13).WithColor(new[] { 1d, 0d, 0d, 1d }),
                ElementBounds.Fixed(2, 60, 180, 20))
            .EndIf()
            .AddStaticText($"{Util.LangStr("charges-text")}:", CairoFont.WhiteSmallText().WithFontSize(15),
                chargesTextBounds,
                "chargesText")
            .AddDynamicText(_blockEntity.ChargeCount.ToString(), CairoFont.WhiteSmallText().WithFontSize(15),
                chargeCountBounds, "chargeCount")
            .AddStaticText("Received Mail", CairoFont.WhiteSmallText(), ElementBounds.Fixed(2, 80, 200, 20),
                "receivedMailTitle")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 0, 1, 2, 3 }, receivedMailBounds,
                "receivedMailBounds")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 4, 5, 6, 7 }, receivedMailBounds2,
                "receivedMailBounds2")
            .EndChildElements()
            .Compose();


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

    public void Redraw()
    {
        SetupDialog();
    }
}