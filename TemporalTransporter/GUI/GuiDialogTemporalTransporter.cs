using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TemporalTransporter.GUI;

public class GuiDialogTemporalTransporter : GuiDialogBlockEntity
{
    private EnumPosFlag _screenPos;

    public GuiDialogTemporalTransporter(InventoryBase inventory, BlockPos bePos, ICoreClientAPI capi) :
        base("Temporal Transporter Viewer", inventory, bePos, capi)
    {
        capi.Input.RegisterHotKey(
            "redraw",
            "Redraw",
            GlKeys.N,
            HotkeyType.GUIOrOtherControls
        );
        capi.Input.SetHotKeyHandler("redraw", _ => SetupDialog());
    }

    public override string ToggleKeyCombinationCode => null!;

    private bool SetupDialog()
    {
        var stoveBounds = ElementBounds.Fixed(0, 0, 210, 250);


        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(stoveBounds);

        var dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithFixedAlignmentOffset(
                    IsRight(_screenPos) ? -GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0)
                .WithAlignment(IsRight(_screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle)
            ;


        var cookingSlotsSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 30 + 45, 4, 1);
        cookingSlotsSlotBounds.fixedHeight += 10;

        var top = cookingSlotsSlotBounds.fixedHeight + cookingSlotsSlotBounds.fixedY;

        var outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153, top, 1, 1);

        SingleComposer = capi.Gui
            .CreateCompo("temporaltransportergui", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .AddDynamicText("", CairoFont.WhiteDetailText(), ElementBounds.Fixed(0, 30, 210, 45), "outputText")
            .AddItemSlotGrid(Inventory, SendInvPacket, 1, new[] { 2 }, outputSlotBounds, "outputslot")
            .AddItemSlotGrid(Inventory, SendInvPacket, 4, new[] { 2 }, cookingSlotsSlotBounds, "cookingSlotsSlotBounds")
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
        base.OnGuiOpened();

        _screenPos = GetFreePos("smallblockgui");
        OccupyPos("smallblockgui", _screenPos);
        SetupDialog();
    }


    public override void OnGuiClosed()
    {
        base.OnGuiClosed();

        FreePos("smallblockgui", _screenPos);
    }
}