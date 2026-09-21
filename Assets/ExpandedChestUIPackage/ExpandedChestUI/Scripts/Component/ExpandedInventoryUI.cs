#define PUG_ACHIEVEMENTS

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ExpandedChestUI.Scripts.System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Component
{
    public class ExpandedInventoryUI : ChestInventoryUI, IScrollable
    {
        [Header("Expanded Inventory UI")]
        public ButtonUIElement optionalPutAllButton;
        public ButtonUIElement optionalSplitStackButton;
        public ButtonUIElement optionalToInventoryButton;
        public ButtonUIElement optionalTakeAllButton;
        public SpriteRenderer backgroundSR2;
        public GameObject root;
        private GameObject Root => root;
        private float _inputFieldWasSetTimer;
        private bool _pendingProfanityCheck;
        private string _signText;

        private int _previousInventorySize = -1;

        private int _amountOfActiveSlots;
        private ObjectCategoryTag _currentRequirementTag;

        public int maxColumns;
        public int maxRows;
        public override int MAX_COLUMNS => maxColumns;
        public override int MAX_ROWS => maxRows;

        private static PlayerController Player => Manager.main.player;

        protected override void Awake()
        {
            Root.SetActive(false);
            base.Awake();
        }

        private static WorldLabel ActiveWorldLabel => !Player ? null : Player.activeWorldLabel;

        public override void ShowContainerUI()
        {
            Root.SetActive(true);
            UpdateContainerSize();
            itemSlotsRoot.gameObject.SetActive(true);
            if (scrollWindow != null)
            {
                scrollWindow.enabled = true;
                scrollWindow.ResetScroll();
            }

            labelRoot.gameObject.SetActive(true);
            if (ActiveWorldLabel is null) return;
            int state = ActiveWorldLabel.GetState();
            if (signStateToggle.stateIndex == state) return;
            signStateToggle.SetState(state);
            
            if (Manager.ui.currentSelectedUIElement is not ExpandedInventorySlotUI) return;
            var selectedSlot = (ExpandedInventorySlotUI)Manager.ui.currentSelectedUIElement;
            if (selectedSlot.visibleSlotIndex >= _amountOfActiveSlots)
            {
                Manager.ui.DeselectAnySelectedUIElement();
                firstSlot.Select();
                scrollWindow.enabled = true;
                scrollWindow.ResetScroll();
                Manager.ui.mouse.PlaceMousePositionOnSelectedUIElementWhenControlledByJoystick();
            }
            else
            {
                scrollWindow.MoveScrollToIncludePosition(selectedSlot.localScrollPosition, 0.6875f);
            }
        }

        public override void HideContainerUI()
        {
            Root.SetActive(false);
            _inputFieldWasSetTimer = 0.0f;
            labelRoot.gameObject.SetActive(false);
            base.HideContainerUI();
        }

        private void Update()
        {
            if (!isShowing || !autoPositionSlots) return;
            TriggerInventoryAchievements();
            if (!SlotsNeedsRefresh()) return;
            MarkSlotsAsDirty();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (ActiveWorldLabel is null) return;
            UpdateNameText();
        }

        protected override void UpdateContainerSize()
        {
            if (Manager.main.player is null) return;
            var inventoryHandler = GetInventoryHandler();
            if (inventoryHandler == null) return;
            int size = inventoryHandler.size;
            if (_previousInventorySize == size) return;
            _previousInventorySize = size;
            visibleRows = Mathf.CeilToInt((float)inventoryHandler.size / inventoryHandler.columns);
            visibleColumns = inventoryHandler.columns;
            float sideStartPosition = GetSideStartPosition(visibleColumns);
            int amountOfActiveSlots = _amountOfActiveSlots;
            _amountOfActiveSlots = 0;
            float height = visibleRows * spread;
            UpdateExtendedInventoryBackground(height);
            foreach (var slotUIBase in itemSlots)
            {
                var itemSlot = (ExpandedInventorySlotUI)slotUIBase;
                float slotX = itemSlot.uiSlotXPosition;
                float slotY = itemSlot.uiSlotYPosition;
                if (slotX < visibleColumns && slotY < visibleRows && _amountOfActiveSlots < inventoryHandler.size)
                {
                    itemSlot.visibleSlotIndex = _amountOfActiveSlots;
                    itemSlot.transform.localPosition =
                        new Vector3(sideStartPosition + slotX * spread, -slotY * spread, 0.0f);
                    itemSlot.gameObject.SetActive(true);
                    itemSlot.UpdateSlot();
                    ++_amountOfActiveSlots;
                }
                else
                {
                    itemSlot.visibleSlotIndex = -1;
                    itemSlot.gameObject.SetActive(false);
                }
            }

            if (amountOfActiveSlots != _amountOfActiveSlots) MarkSlotsAsDirty();
            if (inventoryHandler.entityMonoBehaviour is null) return;
            var buttonUIElements = gameObject.GetComponentsInChildren<ExpandedButtonUIElement>(true);
            bool hasScroll = height > scrollWindow.windowHeight;
            scrollWindow.scrollBar.transform.localPosition = new Vector3(-sideStartPosition + 1.25f,
                scrollWindow.scrollBar.transform.localPosition.y, scrollWindow.scrollBar.transform.localPosition.z);
            for (int index = 0; index < buttonUIElements.Length; index++)
            {
                var buttonUIElement = buttonUIElements[index];
                if (inventoryHandler.entityMonoBehaviour is Chest { showSortAndQuickStackButtons: true })
                {
                    buttonUIElement.transform.localPosition = new Vector3(
                        -sideStartPosition + (hasScroll ? 2.25f : 1.75f) + Mathf.FloorToInt(index / 3f) * spread,
                        buttonUIElement.transform.localPosition.y,
                        buttonUIElement.transform.localPosition.z);
                    buttonUIElement.gameObject.SetActive(true);
                }
                else
                {
                    buttonUIElement.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateExtendedInventoryBackground(float height)
        {
            height = Mathf.Min(height, 3 * spread);
            var vector2 = new Vector2(visibleColumns * spread, height);
            if (scrollWindow is null) return;
            scrollWindow.windowWidth = vector2.x;
            if (backgroundSR is null) return;
            backgroundSR.transform.localScale = new Vector3(vector2.x, vector2.y, 1f);
            backgroundSR2.size = new Vector2(vector2.x + 0.425f, vector2.y + 0.425f);
            itemSlotsRoot.transform.parent.parent.localPosition = new Vector3(0.0f, height / 2, 0.0f);
            itemSlotsRoot.transform.localPosition = new Vector3(0.0f, -(spread / 2), 0.0f);
        }

        private bool SlotsNeedsRefresh()
        {
            var inventoryHandler = GetInventoryHandler();
            if (inventoryHandler == null) return false;
            var inventoryRequirementTags = inventoryHandler.GetInventoryRequirementTags();
            var objectCategoryTag = inventoryRequirementTags is not { Count: > 0 }
                ? ObjectCategoryTag.None
                : inventoryRequirementTags[0];
            if (objectCategoryTag == _currentRequirementTag) return false;
            _currentRequirementTag = objectCategoryTag;
            return true;
        }

        [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
        private void MarkSlotsAsDirty()
        {
            foreach (var itemSlot in itemSlots)
            {
                var expandedSlot = (ExpandedInventorySlotUI)itemSlot;
                if (expandedSlot.isShowing) expandedSlot.dirty = true;
            }
        }

        private static ObjectID GetPetObjectIDCategory(ObjectID objectID)
        {
            return objectID switch
            {
                ObjectID.PetSlipperySlimeBlob or ObjectID.PetSlimeBlob or ObjectID.PetPoisonSlimeBlob
                    or ObjectID.PetLavaSlimeBlob or ObjectID.PetPrinceSlimeBlob => ObjectID.PetSlimeBlob,
                _ => objectID
            };
        }

        private void TriggerInventoryAchievements()
        {
            if (containerType != ItemSlotsUIContainerType.ChestInventory
                || Manager.achievements.HasTriggeredAchievement(AchievementID.PetColors))
                return;
            var dictionary = new Dictionary<ObjectID, HashSet<int>>();
            foreach (var itemSlot in itemSlots)
            {
                var expandedSlot = (ExpandedInventorySlotUI)itemSlot;
                if (!expandedSlot.isShowing) continue;
                var containedObject = expandedSlot.GetContainedObject();
                var objectId = containedObject.objectID;
                if (!InventoryHandler.TryGetExtraInventoryData(containedObject.auxDataIndex, out PetSkinCD data))
                    continue;
                var objectIdCategory = GetPetObjectIDCategory(objectId);
                if (!dictionary.ContainsKey(objectIdCategory))
                    dictionary.Add(objectIdCategory, new HashSet<int>());
                dictionary[objectIdCategory]
                    .Add(objectIdCategory == ObjectID.PetSlimeBlob ? (int)objectId : data.skinIndex);
            }

            foreach (var (key, intSet) in dictionary)
            {
                var petSkinInfo = Manager.ui.petInfosTable.GetPetSkinInfo(key);
                if (petSkinInfo == null ||
                    intSet.Count < (key == ObjectID.PetSlimeBlob ? 5 : petSkinInfo.skins.Count)) continue;
                Manager.achievements.TriggerAchievement(AchievementID.PetColors);
                break;
            }
        }

        private new float GetSideStartPosition(int size) => -((size - 1f) / 2f) * spread;

        public new void UpdateContainingElements(float scroll)
        {
        }

        public new bool IsBottomElementSelected()
        {
            var selected = Manager.ui.currentSelectedUIElement;
            return selected != null && itemSlots.FindAll(x => x.uiSlotYPosition == (visibleRows - 1))
                .Exists(x => x == selected);
        }

        public new bool IsTopElementSelected()
        {
            var selected = Manager.ui.currentSelectedUIElement;
            return selected != null && itemSlots.FindAll(x => x.uiSlotYPosition == 0).Exists(x => x == selected);
        }

        public new float GetCurrentWindowHeight() => Mathf.Max(0f, visibleRows * spread);

        public new void QuickStack()
        {
            base.QuickStack();
            AudioManager.Sfx(SfxTableID.inventorySFXSort, transform.position);
        }

        public void QuickStackToInventory()
        {
            var inventoryHandler = GetInventoryHandler();
            if (inventoryHandler is null || Player is null) return;
            inventoryHandler.QuickStack(Player, Player.playerInventoryHandler);
            AudioManager.Sfx(SfxTableID.inventorySFXSort, transform.position);
        }

        public void PutAll()
        {
            var inventoryHandler = GetInventoryHandler();
            if (inventoryHandler is null || Player is null) return;
            var playerInventoryEntity = Player.playerInventoryHandler.inventoryEntity;
            ExpandedChestActionsClient.MoveAllInventoryItems(playerInventoryEntity, inventoryHandler.inventoryEntity,
                true);
            AudioManager.Sfx(SfxTableID.inventorySFXSort, transform.position);
        }

        public void TakeAll()
        {
            var inventoryHandler = GetInventoryHandler();
            if (inventoryHandler is null || Player is null) return;
            var playerInventoryEntity = Player.playerInventoryHandler.inventoryEntity;
            ExpandedChestActionsClient.MoveAllInventoryItems(inventoryHandler.inventoryEntity, playerInventoryEntity,
                isToPlayerInventory: true);
            AudioManager.Sfx(SfxTableID.inventorySFXSort, transform.position);
        }

        public void SplitStack()
        {
            var inventoryHandler = GetInventoryHandler();
            if (inventoryHandler is null || Player is null) return;
            ExpandedChestActionsClient.SplitInventoryStacks(inventoryHandler.inventoryEntity);
            AudioManager.Sfx(SfxTableID.inventorySFXSort, transform.position);
        }

        public new void SetName()
        {
            if (Player == null || Player.activeWorldLabel == null) return;
            _pendingProfanityCheck = true;
            var activeChest = Player.activeWorldLabel;
            string newDescription = _signText = inputField.pugText.GetText();
            Manager.platform.parentalControlManager.RestrictInput(newDescription, filteredName =>
            {
                if (newDescription != _signText || activeChest == null) return;
                Player.playerCommandSystem.SetDescription(activeChest.entity, filteredName);
                _inputFieldWasSetTimer = 1f;
                _pendingProfanityCheck = false;
            });
        }

        public new void SetVisibilityState()
        {
            var player = Manager.main.player;
            if (player == null || player.activeWorldLabel == null) return;
            player.playerCommandSystem.SetWorldLabelVisibility(player.activeWorldLabel.entity, signStateToggle.stateIndex);
        }

        private void UpdateNameText(bool force = false)
        {
            var activeWorldLabel = ActiveWorldLabel;
            if (!isShowing || activeWorldLabel == null || _pendingProfanityCheck)
                return;
            if (_inputFieldWasSetTimer > 0.0)
            {
                _inputFieldWasSetTimer -= Time.deltaTime;
            }
            else
            {
                if (inputField.inputIsActive)
                    return;
                string newChestText = activeWorldLabel.GetName();
                if (string.IsNullOrEmpty(_signText) && !string.IsNullOrEmpty(inputField.pugText.GetText()))
                {
                    inputField.SetInputText("");
                }
                else
                {
                    if (_signText == newChestText && !force)
                        return;
                    _signText = newChestText;
                    inputField.SetInputText("...");
                    _pendingProfanityCheck = true;
                    Manager.platform.parentalControlManager.RestrictInput(newChestText, filteredName =>
                    {
                        if (newChestText != _signText || ActiveWorldLabel == null)
                            return;
                        inputField.SetInputText(filteredName ?? "");
                        _pendingProfanityCheck = false;
                    });
                }
            }
        }
    }
}