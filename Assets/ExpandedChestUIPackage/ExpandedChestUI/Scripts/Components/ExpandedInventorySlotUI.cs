using Pug.UnityExtensions;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts.Components
{
    public class ExpandedInventorySlotUI : InventorySlotUI
    {
        public override float localScrollPosition => transform.localPosition.y + transform.parent.localPosition.y;

        public ExpandedInventoryUI ExpandedInventoryUI => (ExpandedInventoryUI)slotsUIContainer;
        
        private bool ShowHoverWindow => ExpandedInventoryUI.scrollWindow.IsShowingPosition(localScrollPosition, background.size.y / 2f);

        public override bool isVisibleOnScreen => ShowHoverWindow && WithinExpandedScroll() && isActiveAndEnabled &&
                                                  transform.lossyScale.x != 0.0 && transform.lossyScale.y != 0.0;

        public override UIScrollWindow  uiScrollWindow => ExpandedInventoryUI ? ExpandedInventoryUI.scrollWindow : null;
        
        public override void OnSelected()
        {
            uiScrollWindow.MoveScrollToIncludePosition(localScrollPosition, background.size.y / 2f);
            OnSelectSlot();
        }

        private bool WithinExpandedScroll()
        {
            return Manager.input.SystemPrefersKeyboardAndMouse()
                ? IsWithinScrollArea(Manager.ui.mouse.pointer.transform.position)
                : IsWithinScrollArea(transform.position);
        }

        public bool IsWithinScrollArea(Vector3 contained)
        {
            var size = new Vector2(uiScrollWindow.windowWidth, uiScrollWindow.windowHeight);
            var position = uiScrollWindow.transform.position.To2D();
            var rect = new Rect { size = size, center = position };
            return rect.Contains(contained);
        }

        public override UIelement GetAdjacentUIElement(Direction.Id dir, Vector3 currentPosition)
        {
            var adjacentUiElement1 = base.GetAdjacentUIElement(dir, currentPosition);
            var adjacentUiElement2 = ExpandedInventoryUI.GetAdjacentUIElement(dir, currentPosition);
            return adjacentUiElement1 is not SlotUIBase ? adjacentUiElement1 ? adjacentUiElement1 : adjacentUiElement2 :
                adjacentUiElement1 && adjacentUiElement1.isVisibleOnScreen ? adjacentUiElement1 :
                adjacentUiElement2;
        }
    }
}