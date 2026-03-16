using System.Diagnostics.CodeAnalysis;
using Pug.UnityExtensions;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Component
{
    public class ExpandedInventorySlotUI : InventorySlotUI
    {
        public override float localScrollPosition => transform.localPosition.y + transform.parent.localPosition.y;

        public ExpandedInventoryUI ExpandedInventoryUI => (ExpandedInventoryUI)slotsUIContainer;

        private bool ShowHoverWindow =>
            ExpandedInventoryUI.scrollWindow.IsShowingPosition(localScrollPosition, background.size.y / 2f);

        public override bool isVisibleOnScreen => ShowHoverWindow && WithinExpandedScroll() && isActiveAndEnabled &&
                                                  transform.lossyScale.x != 0.0 && transform.lossyScale.y != 0.0;

        public override UIScrollWindow uiScrollWindow => ExpandedInventoryUI ? ExpandedInventoryUI.scrollWindow : null;

        public override void OnSelected()
        {
            uiScrollWindow.MoveScrollToIncludePosition(localScrollPosition, 0.6875f);
            OnSelectSlot();
        }

        private bool WithinExpandedScroll() => !Manager.input.SystemPrefersKeyboardAndMouse() ||
                                               IsWithinScrollArea(Manager.ui.mouse.pointer.transform.position);

        public bool IsWithinScrollArea(Vector3 contained)
        {
            var size = new Vector2(uiScrollWindow.windowWidth, uiScrollWindow.windowHeight);
            var position = uiScrollWindow.transform.position.To2D();
            var rect = new Rect { size = size, center = position };
            return rect.Contains(contained);
        }

        [SuppressMessage("ReSharper", "SwitchStatementMissingSomeEnumCasesNoDefault")]
        public override UIelement GetAdjacentUIElement(Direction.Id dir, Vector3 currentPosition)
        {
            UIelement adjacentUiElement1 = null;
            var adjacentUiElement2 = slotsUIContainer.GetAdjacentUIElement(dir, currentPosition);
            switch (dir)
            {
                case Direction.Id.forward:
                    int index1 = uiSlotIndex - slotsUIContainer.MAX_COLUMNS;
                    if (uiSlotYPosition == 0) break;

                    adjacentUiElement1 = slotsUIContainer.itemSlots[index1];
                    break;
                case Direction.Id.left:
                    int index2 = uiSlotIndex - 1;
                    if (uiSlotXPosition == 0) break;

                    adjacentUiElement1 = slotsUIContainer.itemSlots[index2];
                    break;
                case Direction.Id.back:
                    int index3 = uiSlotIndex + slotsUIContainer.MAX_COLUMNS;
                    if (uiSlotYPosition == slotsUIContainer.visibleRows - 1) break;

                    adjacentUiElement1 = slotsUIContainer.itemSlots[index3];

                    break;
                case Direction.Id.right:
                    int index4 = uiSlotIndex + 1;
                    if (uiSlotXPosition == slotsUIContainer.visibleColumns - 1) break;

                    adjacentUiElement1 = slotsUIContainer.itemSlots[index4];
                    break;
            }

            return adjacentUiElement1 == null || !adjacentUiElement1.isShowing || !adjacentUiElement1.isVisibleOnScreen
                ? adjacentUiElement2
                : adjacentUiElement1;
        }
    }
}