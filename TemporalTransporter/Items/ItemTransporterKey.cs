using System;
using System.Linq;
using Vintagestory.API.Common;

namespace TemporalTransporter.Items;

public class ItemTransporterKey : Item
{
    public override bool ConsumeCraftingIngredients(ItemSlot[] slots, ItemSlot outputSlot, GridRecipe matchingRecipe)
    {
        var code = GenerateRandomAlphanumericString();

        outputSlot.Itemstack.Attributes.SetString("keycode", code);

        return base.ConsumeCraftingIngredients(slots, outputSlot, matchingRecipe);
    }

    private static string GenerateRandomAlphanumericString(int length = 7)
    {
        return new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length)
            .Select(s => s[new Random().Next(s.Length)]).ToArray());
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        var keyCode = itemStack.Attributes.GetString("keycode", "Unknown");

        return $"{base.GetHeldItemName(itemStack)} ({keyCode})";
    }
}