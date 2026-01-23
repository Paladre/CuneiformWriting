using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace CuneiformWriting.Patches
{

    [HarmonyPatch(typeof(BlockEntityPitKiln))]
    [HarmonyPatch("OnFired")]
    public class PatchPitKilnOnFired
    {
        static Dictionary<BlockEntityPitKiln, ITreeAttribute> savedAttrs =
        new Dictionary<BlockEntityPitKiln, ITreeAttribute>();

        static void Prefix(BlockEntityPitKiln __instance)
        {
            // already captured
            if (savedAttrs.ContainsKey(__instance)) return;

            foreach (var slot in __instance.Inventory)
            {
                var stack = slot.Itemstack;

                if (stack?.Collectible?.Code?.Path.Contains("claytablet")
                    == true &&
                    !stack.Collectible.Code.Path.Contains("fired"))
                {
                    savedAttrs[__instance] = stack.Attributes.Clone();

                    //__instance.Api.Logger.Notification("[Cuneiform] Saved attrs from raw tablet");

                    return;
                }
            }
        }

        static void Postfix(BlockEntityPitKiln __instance)
        {
            if (!savedAttrs.TryGetValue(__instance, out var attrs))
                return;

            foreach (var slot in __instance.Inventory)
            {
                var stack = slot.Itemstack;

                if (stack?.Collectible?.Code?.Path.Contains("claytablet")
                    == true &&
                    stack.Collectible.Code.Path.Contains("fired"))
                {
                    stack.Attributes = attrs.Clone();
                    stack.Attributes.SetBool("fired", true);

                    //__instance.Api.Logger.Notification("[Cuneiform] Restored attrs onto fired tablet");

                    break;
                }
            }

            savedAttrs.Remove(__instance);
        }
    }

    [HarmonyPatch(typeof(BlockEntityBeeHiveKiln))]
    [HarmonyPatch("ConvertItemToBurned")]
    public class PatchBeehiveKilnConvertItemToBurned
    {
        static ConditionalWeakTable<ItemSlot, ITreeAttribute> saved
            = new ConditionalWeakTable<ItemSlot, ITreeAttribute>();

        static void Prefix(ItemSlot itemSlot)
        {
            if (itemSlot?.Itemstack == null) return;

            var stack = itemSlot.Itemstack;

            if (stack.Collectible?.Code?.Path.Contains("claytablet") == true &&
                !stack.Collectible.Code.Path.Contains("fired"))
            {
                saved.Remove(itemSlot);
                saved.Add(itemSlot, stack.Attributes.Clone());
            }
        }

        static void Postfix(ItemSlot itemSlot)
        {
            if (itemSlot?.Itemstack == null) return;

            if (!saved.TryGetValue(itemSlot, out var attrs)) return;

            var stack = itemSlot.Itemstack;

            if (stack.Collectible?.Code?.Path.Contains("claytablet") == true)
            {
                stack.Attributes = attrs.Clone();
                stack.Attributes.SetBool("fired", true);
            }

            saved.Remove(itemSlot);
        }
    }
}
