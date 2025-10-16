using ExpandedChestUI.Scripts.Components;
using HarmonyLib;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts.Patches
{
    [HarmonyPatch]
    internal class UIManagerPatch
    {
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.Init))]
        [HarmonyPrefix]
        // ReSharper disable once InconsistentNaming
        private static void SetupChestUIOverride(UIManager __instance)
        {
            var uiTransform = __instance.playerInventoryUI.transform.parent;
            var playerInvUI = __instance.playerInventoryUI;
            var chestInventoryUI = __instance.chestInventoryUI;
            var newInventoryUI = ExpandedChestUI.ChestUIObject.GetComponent<ExpandedInventoryUI>();
            newInventoryUI.itemSlotPrefab.icon.sharedMaterial ??= chestInventoryUI.itemSlotPrefab.icon.sharedMaterial;
            if (newInventoryUI == chestInventoryUI) return;

            GameObject instGameObject = Object.Instantiate(newInventoryUI.gameObject, uiTransform);
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