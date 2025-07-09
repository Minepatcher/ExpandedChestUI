using UnityEngine;

namespace ExpandedChestUI.Scripts.Components
{
    public class ExpandedInventorySlotUI : InventorySlotUI
    {
        public override float localScrollPosition => transform.localPosition.y + transform.parent.localPosition.y;
        
        // ReSharper disable once InconsistentNaming
        private bool showHoverWindow => slotsUIContainer is not null && slotsUIContainer.scrollWindow.IsShowingPosition(localScrollPosition, background.size.y);

        public override bool isVisibleOnScreen => showHoverWindow && isActiveAndEnabled && transform.lossyScale.x != 0.0 && transform.lossyScale.y != 0.0;

        public override UIScrollWindow uiScrollWindow => slotsUIContainer ? slotsUIContainer.scrollWindow : null;
        
        public override void OnSelected()
        {
            slotsUIContainer?.scrollWindow?.MoveScrollToIncludePosition(localScrollPosition, background.size.y / 2f);
            OnSelectSlot();
        }
    }
    
    
}