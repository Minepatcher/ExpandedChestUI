using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts.Components
{
    public class ExpandedInventorySlotUI : InventorySlotUI
    {
        public override float localScrollPosition => transform.localPosition.y + transform.parent.localPosition.y;

        private ExpandedInventoryUI ExpandedInventoryUI => (ExpandedInventoryUI) slotsUIContainer;
        
        private bool ShowHoverWindow => ExpandedInventoryUI != null && ExpandedInventoryUI.uiScrollWindow.IsShowingPosition(localScrollPosition, 0);
        public override bool isVisibleOnScreen => WithinExpandedScroll() && isActiveAndEnabled && transform.lossyScale.x != 0.0 && transform.lossyScale.y != 0.0;

        public override UIScrollWindow uiScrollWindow => slotsUIContainer ? slotsUIContainer.scrollWindow : null;

        public override void OnSelected()
        {
            ExpandedInventoryUI.uiScrollWindow.MoveScrollToIncludePosition(localScrollPosition, 1.375f);
            OnSelectSlot();
        }

        private bool WithinExpandedScroll()
        {
            return Manager.input.SystemPrefersKeyboardAndMouse()
                ? IsWithinScrollArea(Manager.ui.mouse.pointer.transform.position)
                : ShowHoverWindow;
        }
        
        private bool IsWithinScrollArea(Vector3 position1)
        {
            Vector2 size = new Vector2(uiScrollWindow.windowWidth, uiScrollWindow.windowHeight);
            Vector2 vector2 = new Vector2(uiScrollWindow.windowWidth, uiScrollWindow.windowHeight) / 2f;
            Vector3 position2 = uiScrollWindow.transform.position;
            return new Rect(new Vector2(position2.x, position2.y) + uiScrollWindow.windowLocalCenter - vector2, size)
                .Contains(position1);
        }
    }
}