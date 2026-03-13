using ExpandedChestUI.Scripts.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PugMod;
using Object = UnityEngine.Object;

/*  Taken from Minitte's Inventory Size Patch
    https://mod.io/g/corekeeper/m/inventory-size-patch#description
 */

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts.Patches
{
    [HarmonyPatch]
    internal class ExpandedChestUIPatch
    {
        [HarmonyPatch(typeof(PlayerController), "DetectUndiscoveredObjectsInInventory")]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static bool DetectUndiscoveredObjectsInInventory_Prefix(InventoryHandler inventoryHandler, PlayerController __instance)
        {
            // Harmony access tools not allowed, used CoreLib's reflection util instead.
            var field = __instance.GetType()
                .GetMembersChecked()
                .FirstOrDefault(info => info.GetNameChecked().Equals("previousInventoryObjects"));
            if (field == null)
                throw new MissingFieldException(__instance.GetType().GetNameChecked(), "previousInventoryObjects");
            var previousInventoryObjects = (List<ContainedObjectsBuffer>)API.Reflection.GetValue(field, __instance);
            //List<ContainedObjectsBuffer> previousInventoryObjects = __instance.GetValue<List<ContainedObjectsBuffer>>("previousInventoryObjects");

            // Resize previousInventoryObjects so InventoryHandler will fit
            // Prevents array index-out-of-bounds error
            if (inventoryHandler.size <= previousInventoryObjects.Count) return true;
            ExpandedChestUI.Log.LogInfo(
                $"Old Size {previousInventoryObjects.Count} => New Size: {inventoryHandler.size}");
            while (inventoryHandler.size > previousInventoryObjects.Count)
            {
                previousInventoryObjects.Add(default);
            }

            // run the original function;
            return true;
        }

        [HarmonyPatch(typeof(UIManager), nameof(UIManager.Init))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static void SetupChestUIOverride(UIManager __instance)
        {
            var uiTransform = __instance.playerInventoryUI.transform.parent;
            var playerInvUI = __instance.playerInventoryUI;
            var chestInventoryUI = __instance.chestInventoryUI;
            var newInventoryUI = ExpandedChestUI.ChestUIObject.GetComponent<ExpandedInventoryUI>();
            if (newInventoryUI == null)
            {
                ExpandedChestUI.Log.LogError($"{ExpandedChestUI.FriendlyName} failed to find ChestUI");
                return;
            }

            newInventoryUI.itemSlotPrefab.icon.sharedMaterial ??= chestInventoryUI.itemSlotPrefab.icon.sharedMaterial;
            if (newInventoryUI == chestInventoryUI) return;

            var instGameObject = Object.Instantiate(newInventoryUI.gameObject, uiTransform);
            newInventoryUI = instGameObject.GetComponent<ExpandedInventoryUI>();

            __instance.chestInventoryUI = newInventoryUI;

            newInventoryUI.bottomUIElements.Add(playerInvUI);
            newInventoryUI.optionalQuickStackButton.bottomUIElements.Add(playerInvUI);
            newInventoryUI.optionalQuickStackButton.leftUIElements.Add(newInventoryUI);
            newInventoryUI.optionalSortButton.leftUIElements.Add(newInventoryUI);

            playerInvUI.topUIElements.Add(newInventoryUI.optionalQuickStackButton);
            playerInvUI.topUIElements.Add(newInventoryUI);

            ExpandedChestUI.Log.LogInfo($"{ExpandedChestUI.FriendlyName} loaded successfully");
        }
    }
}