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


        [HarmonyPatch(typeof(UIScrollWindow), "TryGetVisibleAdjacentUIElement")]
        [HarmonyPostfix]
        // ReSharper disable all InconsistentNaming
        public static void TryGetVisibleAdjacentUIElement_Prefix(UIScrollWindow __instance,
            Pug.UnityExtensions.Direction.Id dir,
            UIelement currentElement, ref UIelement adjacentElement, ref bool __result)
        {
            if (__instance.name != "ExpandedChestUI") return;
            ExpandedChestUI.Log.LogInfo($"TryGetVisibleAdjacentUIElement: Dir: {dir}, Current Element: {currentElement}");
            if (currentElement != null && currentElement is SlotUIBase slotUIBase)
            {
                ExpandedChestUI.Log.LogInfo($"\nCurrent Element Index: { slotUIBase.visibleSlotIndex }" +
                                            $"\nCurrent Element X Position: { slotUIBase.uiSlotXPosition }" +
                                            $"\nCurrent Element Y Position: { slotUIBase.uiSlotYPosition }");
                var scrollPosition = __instance.scrollingContent.localPosition.y;
                ExpandedChestUI.Log.LogInfo($"\nCurrent Transform: {currentElement.transform.localPosition}" +
                                            $"\nContainer Transform: {currentElement.transform.parent.localPosition} " +
                                            $"\nScroll Transform: {currentElement.transform.parent.parent.localPosition} " +
                                            $"\nScroll Container: {currentElement.transform.parent.parent.parent.localPosition}");
                ExpandedChestUI.Log.LogInfo($"Current Element Position: { currentElement.localScrollPosition }, " +
                                            $"\nScroll Position: {scrollPosition}, " +
                                            $"\nAdd: {currentElement.localScrollPosition + scrollPosition}");
            }
            ExpandedChestUI.Log.LogInfo($"Adjecent Element: {adjacentElement}, Result: {__result}");
            if (adjacentElement != null && adjacentElement is SlotUIBase slotUIBase1)
            {
                ExpandedChestUI.Log.LogInfo($"\nAdjecent Element Index: { slotUIBase1.visibleSlotIndex }" +
                                            $"\nAdjecent Element X Position: { slotUIBase1.uiSlotXPosition }" +
                                            $"\nAdjecent Element Y Position: { slotUIBase1.uiSlotYPosition }");
            }
        }
    }
}