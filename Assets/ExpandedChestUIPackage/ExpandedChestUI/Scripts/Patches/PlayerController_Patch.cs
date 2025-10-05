using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PugMod;

/*  Taken from Minitte's Inventory Size Patch
    https://mod.io/g/corekeeper/m/inventory-size-patch#description
 */

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts.Patches
{
    [HarmonyPatch]
    internal class PlayerControllerPatch
    {
        [HarmonyPatch(typeof(PlayerController),"DetectUndiscoveredObjectsInInventory")]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool DetectUndiscoveredObjectsInInventory_Prefix(InventoryHandler inventoryHandler, PlayerController __instance)
        {
            // Harmony access tools not allowed, used CoreLib's reflection util instead.
            MemberInfo field = __instance.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals("previousInventoryObjects"));
            if (field == null)
                throw new MissingFieldException(__instance.GetType().GetNameChecked(), "previousInventoryObjects");
            List<ContainedObjectsBuffer> previousInventoryObjects = (List<ContainedObjectsBuffer>)API.Reflection.GetValue(field, __instance);
            //List<ContainedObjectsBuffer> previousInventoryObjects = __instance.GetValue<List<ContainedObjectsBuffer>>("previousInventoryObjects");

            // Resize previousInventoryObjects so InventoryHandler will fit
            // Prevents array index out of bounds error
            if (inventoryHandler.size <= previousInventoryObjects.Count) return true;
            ExpandedChestUI.Log.LogInfo($"Old Size {previousInventoryObjects.Count} => New Size: {inventoryHandler.size}");
            while (inventoryHandler.size > previousInventoryObjects.Count)
            {
                previousInventoryObjects.Add(default);
            }
            // run the original function;
            return true;
        }
    }
}