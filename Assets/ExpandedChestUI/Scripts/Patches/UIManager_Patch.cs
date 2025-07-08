using ExpandedChestUI.Scripts.Components;
using HarmonyLib;
using UnityEngine;

namespace ExpandedChestUI.Scripts.Patches
{
    [HarmonyPatch]
    internal class UIManagerPatch
    {
        [HarmonyPatch(typeof(UIManager), nameof(UIManager.Init))]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        private static void SetupChestUIOverride(UIManager __instance)
        {
            Transform uiTransform = __instance.chestInventoryUI.transform.parent;
            ItemSlotsUIContainer playerInvUI = __instance.playerInventoryUI;
            
            GameObject instGameObject = Object.Instantiate(ExpandedChestUI.ChestUIObject, uiTransform);
            ExpandedInventoryUI instInventoryUI = instGameObject.GetComponent<ExpandedInventoryUI>();
            if (instInventoryUI == Manager.ui.chestInventoryUI) return;
            Manager.ui.chestInventoryUI = instInventoryUI;
            
            instInventoryUI.bottomUIElements.Add(playerInvUI);
            instInventoryUI.optionalQuickStackButton.bottomUIElements.Add(playerInvUI);
            playerInvUI.topUIElements.Add(instInventoryUI);
            playerInvUI.topUIElements.Add(instInventoryUI.optionalQuickStackButton);
            ExpandedChestUI.Log.LogInfo($"{ExpandedChestUI.FriendlyName} loaded successfully");
        }
    }
}