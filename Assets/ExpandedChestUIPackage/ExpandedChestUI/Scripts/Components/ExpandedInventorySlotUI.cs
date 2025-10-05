using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts.Components
{
    public class ExpandedInventorySlotUI : InventorySlotUI
    {
        public override float localScrollPosition => transform.localPosition.y + transform.parent.localPosition.y;

        // ReSharper disable once InconsistentNaming
        private bool showHoverWindow => IsMouseWithinScrollArea() && slotsUIContainer is not null &&
                                        slotsUIContainer.scrollWindow.IsShowingPosition(localScrollPosition,
                                            background.size.y);

        public override bool isVisibleOnScreen => showHoverWindow && isActiveAndEnabled &&
                                                  transform.lossyScale.x != 0.0 && transform.lossyScale.y != 0.0;

        public override UIScrollWindow uiScrollWindow => slotsUIContainer ? slotsUIContainer.scrollWindow : null;

        public override void OnSelected()
        {
            slotsUIContainer?.scrollWindow?.MoveScrollToIncludePosition(localScrollPosition, background.size.y / 2f);
            OnSelectSlot();
        }

        private bool IsMouseWithinScrollArea()
        {
            Vector3 position1 = Manager.ui.mouse.pointer.transform.position;
            Vector2 size = new Vector2(uiScrollWindow.windowWidth, uiScrollWindow.windowHeight);
            Vector2 vector2 = new Vector2(uiScrollWindow.windowWidth, uiScrollWindow.windowHeight) / 2f;
            Vector3 position2 = uiScrollWindow.transform.position;
            return new Rect(new Vector2(position2.x, position2.y) + uiScrollWindow.windowLocalCenter - vector2, size)
                .Contains(position1);
        }
    }
}