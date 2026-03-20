using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ExpandedChestUI.Component;
using ExpandedChestUI.Util;
using HarmonyLib;
using Inventory;
using PugMod;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Patch
{
    [HarmonyPatch]
    internal class ExpandedChestUIPatch
    {
        /*  Taken from Minitte's Inventory Size Patch
        https://mod.io/g/corekeeper/m/inventory-size-patch#description
        */
        [HarmonyPatch(typeof(PlayerController), "DetectUndiscoveredObjectsInInventory")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool DetectUndiscoveredObjectsInInventory_Prefix(InventoryHandler inventoryHandler,
            PlayerController __instance)
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
            //ExpandedChestUI.Log.LogInfo($"Old Size {previousInventoryObjects.Count} => New Size: {inventoryHandler.size}");
            while (inventoryHandler.size > previousInventoryObjects.Count)
            {
                previousInventoryObjects.Add(default);
            }

            // run the original function;
            return true;
        }

        [HarmonyPatch(typeof(UIManager), nameof(UIManager.Init))]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static void SetupChestUIOverride(UIManager __instance)
        {
            var uiTransform = __instance.playerInventoryUI.transform.parent;
            var playerInvUI = __instance.playerInventoryUI;
            var chestInventoryUI = __instance.chestInventoryUI;
            var newInventoryUI = ExpandedChestUIMod.ChestUIObject.GetComponent<ExpandedInventoryUI>();
            if (newInventoryUI == null)
            {
                ExpandedChestUIMod.Log.LogError($"{ExpandedChestUIMod.FriendlyName} failed to find ChestUI");
                return;
            }

            newInventoryUI.itemSlotPrefab.icon.sharedMaterial ??= chestInventoryUI.itemSlotPrefab.icon.sharedMaterial;
            if (newInventoryUI == chestInventoryUI) return;

            var instGameObject = Object.Instantiate(newInventoryUI.gameObject, uiTransform);
            newInventoryUI = instGameObject.GetComponent<ExpandedInventoryUI>();

            __instance.chestInventoryUI = newInventoryUI;

            newInventoryUI.bottomUIElements.Add(playerInvUI);
            newInventoryUI.optionalPutAllButton.bottomUIElements.Add(playerInvUI);
            newInventoryUI.optionalTakeAllButton.bottomUIElements.Add(playerInvUI);

            playerInvUI.topUIElements.Add(newInventoryUI.optionalPutAllButton);
            playerInvUI.topUIElements.Add(newInventoryUI.optionalTakeAllButton);
            playerInvUI.topUIElements.Add(newInventoryUI);

            ExpandedChestUIMod.Log.LogInfo($"{ExpandedChestUIMod.FriendlyName} loaded successfully");
        }

        [HarmonyPatch(typeof(InventoryUtility), "TryFindSlotToAddTo")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        private static bool TryFindSlotToAddTo_Prefix(ref int __result,
            in InventoryHandlerShared inventoryHandlerShared,
            ObjectID objectID,
            Entity inventoryEntity,
            int indexHint,
            int endIndex,
            int variation,
            bool isQuickStacking = false)
        {
            if (!ExpandedInventoryUtility.IsPlayerInventory) return true;
            var inventoryBuffer1 = inventoryHandlerShared.inventoryLookup[inventoryEntity];
            var inventorySlotsRequirements =
                inventoryHandlerShared.inventorySlotRequirementBufferLookup[inventoryEntity];
            bool flag1 = inventoryHandlerShared.playerGhostLookup.HasComponent(inventoryEntity);
            bool flag2 = isQuickStacking;
            var dynamicBuffer1 = inventoryHandlerShared.containedObjectsBufferLookup[inventoryEntity];
            bool buffer =
                inventoryHandlerShared.lockedObjectsBufferLookup.TryGetBuffer(inventoryEntity, out var dynamicBuffer2);
            bool flag3 = !InventoryUtility.CheckIfCanOnlyContainOneItemPerSlot(inventoryBuffer1);
            int num1 = -1;
            int num2 = -1;
            for (int index = 0; index < inventoryBuffer1.Length; ++index)
            {
                var inventoryBuffer2 = inventoryBuffer1[index];
                int startIndex = index == 0 && ExpandedInventoryUtility.IsPlayerInventory ? 10 : 0;
                bool flag4 = false;
                bool flag5 =
                    PugDatabase.GetEntityObjectInfo(objectID, inventoryHandlerShared.databaseBankCD.databaseBankBlob)
                        .isStackable && !InventoryUtility.CheckIfCantAddObjectsToInventory(inventoryBuffer1, indexHint);
                int num3 = endIndex;
                int size = inventoryBuffer2.size;
                int num4;
                if (num3 == -1 && indexHint >= size)
                {
                    num4 = indexHint;
                    num3 = indexHint + 1;
                }
                else if (num3 <= size)
                {
                    num4 = inventoryBuffer2.startIndex;
                    num3 = inventoryBuffer2.startIndex + size;
                }
                else
                    num4 = math.max(inventoryBuffer2.startIndex, indexHint);

                num4 = math.max(num4, startIndex);

                var primaryPrefabEntity = PugDatabase.GetPrimaryPrefabEntity(objectID,
                    inventoryHandlerShared.databaseBankCD.databaseBankBlob, variation);
                var categoryTagsLookup =
                    inventoryHandlerShared.objectCategoryTagsLookup;
                ObjectCategoryTagsCD objectCategoryTagsCd;
                if (!categoryTagsLookup.HasComponent(primaryPrefabEntity))
                {
                    objectCategoryTagsCd = new ObjectCategoryTagsCD();
                }
                else
                {
                    categoryTagsLookup = inventoryHandlerShared.objectCategoryTagsLookup;
                    objectCategoryTagsCd = categoryTagsLookup[primaryPrefabEntity];
                }

                var objectTagCD = objectCategoryTagsCd;
                for (int checkSpecificIndexOnly = num4; checkSpecificIndexOnly < num3; ++checkSpecificIndexOnly)
                {
                    if (flag1 & isQuickStacking)
                        flag2 = (index != 0 || checkSpecificIndexOnly >= 10) && isQuickStacking;
                    if (isQuickStacking & buffer && dynamicBuffer2[checkSpecificIndexOnly].Value ||
                        !InventoryUtility.ObjectIsValidToPutInInventory(inventorySlotsRequirements, objectTagCD,
                            objectID, inventoryBuffer1, inventoryHandlerShared.overrideAlwaysAllowToBeTrashedLookup,
                            out int indexFulfillingRequirements, inventoryHandlerShared.databaseBankCD,
                            checkSpecificIndexOnly)) continue;
                    var objectId = dynamicBuffer1[checkSpecificIndexOnly].objectData.objectID;
                    bool flag6 = flag3 & flag5 && objectId == objectID &&
                                 dynamicBuffer1[checkSpecificIndexOnly].objectData.variation == variation &&
                                 dynamicBuffer1[checkSpecificIndexOnly].objectData.amount < 9999;
                    if (num1 == -1 && objectId == ObjectID.None || !flag4 & flag6)
                    {
                        num1 = checkSpecificIndexOnly;
                        flag4 = flag6;
                    }

                    if (objectId == ObjectID.None && indexFulfillingRequirements > -1 && num2 == -1)
                        num2 = indexFulfillingRequirements;
                    if ((indexHint != checkSpecificIndexOnly || !(objectId == ObjectID.None | flag6)) &&
                        !(indexHint < checkSpecificIndexOnly & flag6)) continue;
                    if (!flag2 || indexFulfillingRequirements != -1)
                    {
                        __result = checkSpecificIndexOnly;
                        return false;
                    }

                    num1 = checkSpecificIndexOnly;
                }
            }

            __result = num2 == -1 ? num1 : num2;
            return false;
        }

        [HarmonyPatch(typeof(PlayerController), "UpdateInventoryStuff")]
        [HarmonyPrefix]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static bool UpdateInventoryStuff_Prefix(PlayerController __instance)
        {
            if (__instance.activeInventoryHandler == null ||
                __instance.activeInventoryHandler == __instance.playerInventoryHandler ||
                __instance.activeInventoryHandler != Manager.ui.chestInventoryUI.GetInventoryHandler()
                )
                return true;
            bool returnToMethod = false;
            if ((__instance.inputModule.PrefersKeyboardAndMouse() &&
                 __instance.inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.HOT_BAR_SWAP_MODIFIER) ||
                 !__instance.inputModule.PrefersKeyboardAndMouse() &&
                 __instance.inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.QUICK_MOVE_ITEMS)) &&
                __instance.inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.QUICK_STACK))
            {
                ((ExpandedInventoryUI)Manager.ui.chestInventoryUI).TakeAll();
            }
            else if ((__instance.inputModule.PrefersKeyboardAndMouse() &&
                      __instance.inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.HOT_BAR_SWAP_MODIFIER) ||
                      !__instance.inputModule.PrefersKeyboardAndMouse() &&
                      __instance.inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.QUICK_MOVE_ITEMS)) &&
                     __instance.inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.SORT))
            {
                ((ExpandedInventoryUI)Manager.ui.chestInventoryUI).PutAll();
            }
            else if ((__instance.inputModule.PrefersKeyboardAndMouse() &&
                      __instance.inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.QUICK_MOVE_ITEMS) ||
                      !__instance.inputModule.PrefersKeyboardAndMouse() &&
                      __instance.inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.DROP_SELECTED_ITEM)) &&
                     __instance.inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.SORT))
            {
                ((ExpandedInventoryUI)Manager.ui.chestInventoryUI).SplitStack();
            }
            else if ((__instance.inputModule.PrefersKeyboardAndMouse() &&
                      __instance.inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.QUICK_MOVE_ITEMS) ||
                      !__instance.inputModule.PrefersKeyboardAndMouse() &&
                      __instance.inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.DROP_SELECTED_ITEM)) &&
                     __instance.inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.QUICK_STACK))
            {
                ((ExpandedInventoryUI)Manager.ui.chestInventoryUI).QuickStackToInventory();
            }
            else if (__instance.inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.QUICK_STACK))
            {
                ((ExpandedInventoryUI)Manager.ui.chestInventoryUI).QuickStack();
            }
            else if (Manager.ui.currentSelectedUIElement != null && Manager.ui.currentSelectedUIElement.GetComponentInParent(typeof(ExpandedInventoryUI)) && __instance.inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.SORT))
            {
                ((ExpandedInventoryUI)Manager.ui.chestInventoryUI).Sort();
            }
            else if (__instance.inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.SORT))
            {
                ((InventoryUI)Manager.ui.playerInventoryUI).Sort();
            }
            else
            {
                returnToMethod = true;
            }

            return returnToMethod;
        }
    }
}