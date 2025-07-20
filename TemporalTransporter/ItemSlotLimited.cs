using System.Linq;
using Vintagestory.API.Common;

namespace TemporalTransporter;

public class ItemSlotLimited : ItemSlot
{
    private readonly string[] _allowedItems;

    public ItemSlotLimited(InventoryBase inv, string[] allowedItems) : base(inv)
    {
        _allowedItems = allowedItems;
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