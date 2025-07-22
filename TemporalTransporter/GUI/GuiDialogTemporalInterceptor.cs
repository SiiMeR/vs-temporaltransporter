using TemporalTransporter.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.GUI;

public class GuiDialogTemporalInterceptor : GuiDialogBlockEntity
{
    private readonly BlockEntityTemporalInterceptor _blockEntity;
    private bool _isDisabled;

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

        var receivedMailBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 50, 4, 1);
        var receivedMailBounds2 =
            ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, receivedMailBounds.fixedY + 50, 4, 1);

        SingleComposer = capi.Gui
            .CreateCompo("temporalinterceptorgui", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .AddIf(_isDisabled)
            .AddStaticText("Disabled: Not visible from sky",
                CairoFont.WhiteSmallText().WithFontSize(14).WithColor(new[] { 1d, 0d, 0d, 1d }),
                ElementBounds.Fixed(0, 100, 210, 20))
            .EndIf()
            .AddStaticText("Received Mail", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 20, 200, 20),
                "receivedMailTitle")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 0, 1, 2, 3 }, receivedMailBounds,
                "receivedMailBounds")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 4, 5, 6, 7 }, receivedMailBounds2,
                "receivedMailBounds2")
            .EndChildElements()
            .Compose();


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
}