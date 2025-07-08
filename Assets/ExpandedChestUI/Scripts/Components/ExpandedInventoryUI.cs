using System.Collections.Generic;
using UnityEngine;

namespace ExpandedChestUI.Scripts.Components
{
    public class ExpandedInventoryUI : InventoryUI, IScrollable
    {
        public SpriteMask scrollMask;
        public GameObject root;
        private GameObject Root => root;
        
        private int _previousInventorySize = -1;

        private int _amountOfActiveSlots;
        private ObjectCategoryTag _currentRequirementTag;
        
        public int maxColumns;
        public int maxRows;
        public override int MAX_COLUMNS => maxColumns;
        public override int MAX_ROWS => maxRows;

        protected override void Awake()
        {
            Root.SetActive(false);
            base.Awake();
        }

        public override void ShowContainerUI()
        {
            Root.SetActive(true);
            UpdateContainerSize();
            itemSlotsRoot.gameObject.SetActive(true);
            if (scrollWindow is not null)
                scrollWindow.enabled = true;
            foreach (SlotUIBase itemSlot in itemSlots)
            {
                if (itemSlot.gameObject.activeInHierarchy)
                    itemSlot.UpdateSlot();
            }
        }

        public override void HideContainerUI()
        {
            Root.SetActive(false);
            base.HideContainerUI();
        }

        private void Update()
        {
            if (!isShowing || !autoPositionSlots) return;
            UpdateContainerSize();
            TriggerInventoryAchievements();
            if (!SlotsNeedsRefresh()) return;
            MarkSlotsAsDirty();
        }

        private void UpdateContainerSize()
        {
            if (Manager.main.player is null) return;
            InventoryHandler inventoryHandler = GetInventoryHandler();
            if (inventoryHandler == null) return;
            int size = inventoryHandler.size;
            if (_previousInventorySize == size) return;
            _previousInventorySize = size;
            visibleRows = Mathf.CeilToInt((float)inventoryHandler.size / inventoryHandler.columns);
            visibleColumns = inventoryHandler.columns;
            float sideStartPosition = GetSideStartPosition(visibleColumns);
            int amountOfActiveSlots = _amountOfActiveSlots;
            _amountOfActiveSlots = 0;
            foreach (SlotUIBase itemSlot in itemSlots)
            {
                float slotX = itemSlot.uiSlotXPosition;
                float slotY = itemSlot.uiSlotYPosition;
                if (slotX < visibleColumns && slotY < visibleRows && _amountOfActiveSlots < inventoryHandler.size)
                {
                    itemSlot.visibleSlotIndex = _amountOfActiveSlots;
                    itemSlot.transform.localPosition = new Vector3(sideStartPosition + slotX * spread, -slotY * spread, 0.0f);
                    itemSlot.gameObject.SetActive(true);
                    itemSlot.UpdateSlot();
                    ++_amountOfActiveSlots;
                }
                else
                    itemSlot.gameObject.SetActive(false);
            }
            if (amountOfActiveSlots != _amountOfActiveSlots) MarkSlotsAsDirty();
            float height = visibleRows * spread;
            UpdateExtendedInventoryBackground(height);
            if (inventoryHandler.entityMonoBehaviour is null) return;
            switch (inventoryHandler.entityMonoBehaviour)
            {
                case Chest { showSortAndQuickStackButtons: true }:
                    bool hasScroll = height > scrollWindow.windowHeight;
                    if (hasScroll)
                    {
                        scrollWindow.scrollBar.transform.localPosition = new Vector3(
                            -sideStartPosition + 0.85f,
                            scrollWindow.scrollBar.transform.localPosition.y,
                            scrollWindow.scrollBar.transform.localPosition.z);
                    }
                    
                    if (optionalQuickStackButton is not null)
                    {
                        optionalQuickStackButton.transform.localPosition = new Vector3(
                            -sideStartPosition + (hasScroll ? 1.8f : spread),
                            optionalQuickStackButton.transform.localPosition.y,
                            optionalQuickStackButton.transform.localPosition.z);
                        optionalQuickStackButton.gameObject.SetActive(true);
                    }

                    if (optionalSortButton is not null)
                    {
                        optionalSortButton.transform.localPosition = new Vector3(
                            -sideStartPosition + (hasScroll ? 1.8f : spread),
                            optionalSortButton.transform.localPosition.y, optionalSortButton.transform.localPosition.z);
                        optionalSortButton.gameObject.SetActive(true);
                    }
                    break;
                default:
                    optionalQuickStackButton?.gameObject.SetActive(false);
                    optionalSortButton?.gameObject.SetActive(false);
                    break;
            }
        }

        private void UpdateExtendedInventoryBackground(float height)
        {
            height = Mathf.Min(height, 3 * spread);
            Vector2 vector2 = new Vector2(visibleColumns * spread, height);
            if (backgroundSR is not null) backgroundSR.size = vector2;
            if (backgroundBlockCollider is not null) backgroundBlockCollider.size = new Vector3(vector2.x, vector2.y, backgroundBlockCollider.size.z);
            if (scrollMask is not null) scrollMask.transform.localScale = new Vector3(vector2.x, vector2.y, scrollMask.transform.localScale.z);
            if (scrollWindow is null) return;
            scrollWindow.windowWidth = vector2.x;
            scrollWindow.windowHeight = vector2.y;
            scrollWindow.ResetScroll();
            if (backgroundSR is not null) itemSlotsRoot.transform.parent.parent.localPosition = new Vector3(0.0f, backgroundSR.localBounds.max.y - spread / 2, 0.0f);
        }

        private bool SlotsNeedsRefresh()
        {
            InventoryHandler inventoryHandler = GetInventoryHandler();
            if (inventoryHandler == null) return false;
            List<ObjectCategoryTag> inventoryRequirementTags = inventoryHandler.GetInventoryRequirementTags();
            ObjectCategoryTag objectCategoryTag = inventoryRequirementTags is not { Count: > 0 } ? ObjectCategoryTag.None : inventoryRequirementTags[0];
            if (objectCategoryTag == _currentRequirementTag) return false;
            _currentRequirementTag = objectCategoryTag;
            return true;
        }

        private void MarkSlotsAsDirty()
        {
            foreach (SlotUIBase itemSlot in itemSlots)
            {
                if (itemSlot.isShowing) ((InventorySlotUI)itemSlot).dirty = true;
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
            if (containerType != ItemSlotsUIContainerType.ChestInventory || Manager.achievements.HasTriggeredAchievement(AchievementID.PetColors))
                return;
            Dictionary<ObjectID, HashSet<int>> dictionary = new Dictionary<ObjectID, HashSet<int>>();
            foreach (SlotUIBase itemSlot in itemSlots)
            {
                if (!itemSlot.isShowing) continue;
                ContainedObjectsBuffer containedObject = itemSlot.GetContainedObject();
                ObjectID objectId = containedObject.objectID;
                if (!InventoryHandler.TryGetExtraInventoryData(containedObject.auxDataIndex, out PetSkinCD data))
                    continue;
                ObjectID objectIdCategory = GetPetObjectIDCategory(objectId);
                if (!dictionary.ContainsKey(objectIdCategory))
                    dictionary.Add(objectIdCategory, new HashSet<int>());
                dictionary[objectIdCategory].Add(objectIdCategory == ObjectID.PetSlimeBlob ? (int) objectId : data.skinIndex);
            }
            foreach ((ObjectID key, HashSet<int> intSet) in dictionary)
            {
                PetInfosTable.PetSkinInfo petSkinInfo = Manager.ui.petInfosTable.GetPetSkinInfo(key);
                if (petSkinInfo == null ||
                    intSet.Count < (key == ObjectID.PetSlimeBlob ? 5 : petSkinInfo.skins.Count)) continue;
                Manager.achievements.TriggerAchievement(AchievementID.PetColors);
                break;
            }
        }

        private float GetSideStartPosition(int size)
        {
            return (float) -((size - 1) / 2.0) * spread;
        }
        
        public new void UpdateContainingElements(float scroll)
        {
        }

        public new bool IsBottomElementSelected()
        {
            if (Manager.ui.currentSelectedUIElement is null) return false;
            int index = itemSlots.FindIndex(x => x == Manager.ui.currentSelectedUIElement) + 1;
            int lastColumnIndex = _amountOfActiveSlots - visibleColumns + 1;
            return index >= lastColumnIndex && index <= _amountOfActiveSlots;
        }

        public new bool IsTopElementSelected()
        {
            if (Manager.ui.currentSelectedUIElement is null) return false;
            int index = itemSlots.FindIndex(x => x == Manager.ui.currentSelectedUIElement) + 1;
            return index <= visibleColumns;
        }

        public new float GetCurrentWindowHeight()
        {
            return visibleRows * spread;
        }
    }
}