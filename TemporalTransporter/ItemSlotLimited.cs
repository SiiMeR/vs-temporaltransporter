using System.Linq;
using Vintagestory.API.Common;

namespace TemporalTransporter;

public class ItemSlotLimited : ItemSlot
{
    private readonly string[] _allowedItems;
    private bool _canTake;

    public ItemSlotLimited(InventoryBase inv, string[] allowedItems, bool canTake) : base(inv)
    {
        _allowedItems = allowedItems;
        _canTake = canTake;
    }

    public void SetCanTake(bool canTake)
    {
        _canTake = canTake;
    }

    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
        return false;
    }

    public override bool CanTake()
    {
        return _canTake;
    }

    public override bool CanHold(ItemSlot? sourceSlot)
    {
        if (sourceSlot?.Itemstack == null)
        {
            return false;
        }

        var code = sourceSlot.Itemstack.Collectible.Code.ToString();

        return _allowedItems.Any(allowed => code == allowed) && base.CanHold(sourceSlot);
    }
}