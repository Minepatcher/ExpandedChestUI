using System;
using System.Collections.Generic;
using Pug.Sprite;
using Pug.UnityExtensions;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts.Components
{
    public class ExpandedUIScrollWindow : UIScrollWindow
    {
        private IScrollable _scrollable;
        private float _previousPosition = -1f;
        private float _previousHeight = -1f;
        private bool _isCreditsMenu;
        private readonly List<SpriteObject> _spriteObjects = new();
        private readonly List<Vector3> _spriteObjectStartPositions = new();
        private Vector3 _spriteObjectOffset = Vector3.zero;

        public new float VisibleRatio => windowHeight / (windowHeight + ScrollHeight);

        public new float ScrollHeight { get; private set; }

        private void Awake()
        {
            if (scrollable is IScrollable scrollable1)
            {
                _scrollable = scrollable1;
            }
            else
            {
                Debug.LogError(scrollable +
                               " does not implement IScrollable, disabling UIScrollWindow");
                enabled = false;
            }

            if (!(scrollable != null) ||
                scrollable is not RadicalCreditsMenu)
                return;
            _isCreditsMenu = true;
            foreach (var componentsInChild in transform.GetComponentsInChildren<SpriteObject>(true))
            {
                _spriteObjects.Add(componentsInChild);
                _spriteObjectStartPositions.Add(componentsInChild.transform.localPosition);
            }
        }

        private void LateUpdate()
        {
            if (_scrollable == null) return;
            UpdateScrollHeight();
            UpdateScroll();
            if (Math.Abs(_previousPosition - scrollingContent.localPosition.y) <= 1.0 / 1000.0 &&
                Math.Abs(_previousHeight - ScrollHeight) <= 1.0 / 1000.0)
                return;
            UpdateArrows();
            UpdateScrollbar();
            _previousPosition = scrollingContent.localPosition.y;
            _previousHeight = ScrollHeight;
        }

        public new void ResetScroll() => SetScrollValue(1f);

        private bool IsMouseWithinScrollArea()
        {
            var position1 = Manager.ui.mouse.pointer.transform.position;
            var size = new Vector2(windowWidth, windowHeight);
            var position = transform.position.To2D();
            var rect = new Rect { size = size, center = position };
            return rect.Contains(position1);
        }

        private void UpdateScroll()
        {
            if (VisibleRatio >= 1.0)
            {
                float num = minScrollPos;
                if (centerVertically)
                    num = math.lerp(minScrollPos,
                        minScrollPos + windowHeight - _scrollable.GetCurrentWindowHeight(), 0.5f);
                SetScrollablePosition(-num);
            }
            else
            {
                bool flag1 = !Manager.input.SystemPrefersKeyboardAndMouse();
                var currentElement = Manager.ui.currentSelectedUIElement;
                bool flag2 = !flag1
                    ? !cursorMustBeInsideWindowToScroll || IsMouseWithinScrollArea() &&
                    scrollable is UIelement { isShowing: true }
                    : !anyElementMustBeSelectedToScrollWithController |
                      (currentElement != null &&
                       currentElement.uiScrollWindow == this);
                float num1 = 0.0f;
                float num2 = 0.0f;
                if (flag2)
                {
                    num1 = -Manager.input.GetScrollValue();
                    num2 = scrollMultiplierSpeedMouse;
                    if (flag1)
                        num2 = scrollMultiplierSpeedJoystick;
                    if (allowScrollWithArrowKeys)
                    {
                        float num3 = Manager.input.IsMenuUpButtonPressed()
                            ? -5f
                            : (Manager.input.IsMenuDownButtonPressed() ? 5f : 0.0f);
                        if (num3 != 0.0)
                            num1 = (float)(Time.unscaledDeltaTime * (double)num3 * 5.0);
                    }

                    if (flag1)
                        num1 *= Time.unscaledDeltaTime * 50f;
                }

                MoveScroll(num1 * num2);
                if (!flag1)
                    return;
                bool flag3 = false;
                float num4 = -Manager.input.singleplayerInputModule.GetRawAxisInput().y;
                if (scrollWithLeftStick || _scrollable.IsTopElementSelected() && num4 < 0.0 || _scrollable.IsBottomElementSelected() && num4 > 0.0)
                    flag3 = true;
                if (flag3 && Math.Abs(num4) > 0.10000000149011612)
                    MoveScroll((float)(num4 * (double)num2 * Time.unscaledDeltaTime * 50.0));
                if (!flag2 || !(currentElement != null) ||
                    IsShowingPosition(currentElement.localScrollPosition))
                    return;
                if (TryGetVisibleAdjacentUIElement(
                        currentElement.localScrollPosition +
                        (double)scrollingContent.transform.localPosition.y > 0.0
                            ? Direction.Id.back
                            : Direction.Id.forward, currentElement, out var adjacentElement))
                {
                    adjacentElement.Select();
                    currentElement = adjacentElement;
                    Manager.ui.mouse.PlaceMousePositionOnSelectedUIElementWhenControlledByJoystick();
                }

                if (!(currentElement != null) ||
                    !IsShowingPosition(currentElement.localScrollPosition, padding))
                    return;
                currentElement.Select();
                Manager.ui.mouse.PlaceMousePositionOnSelectedUIElementWhenControlledByJoystick();
            }
        }

        private bool TryGetVisibleAdjacentUIElement(
            Direction.Id dir,
            UIelement currentElement,
            out UIelement adjacentElement)
        {
            for (int index = 0;
                 index < 100 && !(currentElement == null) &&
                 !IsShowingPosition(currentElement.localScrollPosition, padding);
                 ++index)
                currentElement = currentElement.GetAdjacentUIElement(dir, currentElement.transform.position);
            adjacentElement = currentElement;
            return currentElement != null &&
                   IsShowingPosition(currentElement.localScrollPosition, padding);
        }

        private void SetScrollablePosition(float verticalOffsetFromParent)
        {
            if (!dontForcePixelPerfect)
                verticalOffsetFromParent = Mathf.Round(verticalOffsetFromParent / (1f / 16f)) * (1f / 16f);
            var vector3 = scrollingContent.localPosition;
            vector3.y = verticalOffsetFromParent;
            scrollingContent.localPosition = vector3;
            _scrollable.UpdateContainingElements(verticalOffsetFromParent);
        }

        public new void SetScrollValue(float normalizedScrollValue)
        {
            if (_scrollable == null) return;
            SetScrollablePosition(math.lerp(ScrollHeight, minScrollPos, normalizedScrollValue));
        }

        public new void MoveScroll(float scrollValue)
        {
            if (_scrollable == null) return;
            float verticalOffsetFromParent = Math.Clamp(scrollingContent.localPosition.y + scrollValue,
                minScrollPos, ScrollHeight);
            if (_isCreditsMenu)
            {
                _spriteObjectOffset.y = math.abs(scrollValue * 0.5f);
                for (int index = 0; index < _spriteObjects.Count; ++index)
                    _spriteObjects[index].transform.localPosition =
                        _spriteObjectStartPositions[index] + _spriteObjectOffset;
            }

            SetScrollablePosition(verticalOffsetFromParent);
            Manager.ui.mouse.PlaceMousePositionOnSelectedUIElementWhenControlledByJoystick();
        }

        private void UpdateScrollHeight()
        {
            ScrollHeight = math.max(0.0f, _scrollable.GetCurrentWindowHeight() - windowHeight);
        }

        private void UpdateArrows()
        {
            if (arrowUp != null)
            {
                bool flag = scrollingContent.localPosition.y - 1.0 / 16.0 > 0.0;
                arrowUp.gameObject.SetActive(flag);
                if (arrowUpInactive != null)
                    arrowUpInactive.gameObject.SetActive(!flag);
            }

            if (!(arrowDown != null))
                return;
            bool flag1 = scrollingContent.localPosition.y + 1.0 / 16.0 < ScrollHeight;
            arrowDown.gameObject.SetActive(flag1);
            if (!(arrowDownInactive != null))
                return;
            arrowDownInactive.gameObject.SetActive(!flag1);
        }

        private void UpdateScrollbar()
        {
            if (scrollBar == null) return;
            if (VisibleRatio >= 1.0)
            {
                ExpandedChestUI.Log.LogInfo("VisibleRatio >= 1.0");
                if (!autoHideScrollbar)
                    return;
                scrollBar.gameObject.SetActive(false);
            }
            else
            {
                ExpandedChestUI.Log.LogInfo("VisibleRatio < 1.0");
                scrollBar.gameObject.SetActive(true);
                scrollBar.UpdateScrollBarPosition(math.clamp(
                    (float)(1.0 - (scrollingContent.localPosition.y - (double)minScrollPos) /
                        (ScrollHeight - (double)minScrollPos)), 0.0f, 1f));
            }
        }

        public new void MoveScrollToIncludePosition(float positionRelativeScrollRoot, float padding2)
        {
            if (Manager.input.SystemPrefersKeyboardAndMouse() && (!Manager.input.SystemIsUsingKeyboard() ||
                                                                  !Manager.input.IsMenuDownButtonPressed() &&
                                                                  !Manager.input.IsMenuUpButtonPressed()))
                return;
            float num = scrollingContent.localPosition.y + positionRelativeScrollRoot;
            if (num - (double)padding2 < -(double)windowHeight)
            {
                MoveScroll(-windowHeight - num + padding2);
            }
            else
            {
                if (num + (double)padding2 <= 0.0)
                    return;
                MoveScroll((float)-(num + (double)padding2));
            }
        }

        public new bool IsShowingPosition(float posToInclude, float padding2 = 0.0f)
        {
            float num = scrollingContent.localPosition.y + posToInclude;
            return num + (double)padding2 >= -(double)windowHeight && num - (double)padding2 <= 0.0;
        }

        public new bool IsAtBottom()
        {
            return scrollingContent.localPosition.y + 1.0 / 16.0 >= ScrollHeight;
        }
    }
}