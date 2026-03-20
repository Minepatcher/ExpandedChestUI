using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Component
{
    public class ExpandedButtonUIElement : ButtonUIElement
    {
        [FormerlySerializedAs("optionalShortCut2")] [Header("Expanded Button UI Element")] 
        public ExpandedShortCutBinding optionalShortCutModifier;
        private readonly string[] _bindingTerms = {
            "",
            "Sort",
            "QuickStack",
            "QuickMoveItems",
            "HotbarSwapModifier",
            "DropSelectedItem"
        };

        public override List<TextAndFormatFields> GetHoverDescription()
        {
            if (!showHoverDesc)
                return base.GetHoverDescription();
            bool preferJoystick = Manager.input.IsAnyGamepadConnected() && !Manager.input.singleplayerInputModule.PrefersKeyboardAndMouse();
            string shortCutString = Manager.ui.GetShortCutString(_bindingTerms[(int)optionalShortCut], preferJoystick);
            string shortCutString2 = optionalShortCutModifier switch
            {
                ExpandedShortCutBinding.Shift => preferJoystick
                    ? Manager.ui.GetShortCutString(_bindingTerms[5], true)
                    : Manager.ui.GetShortCutString(_bindingTerms[3], false),
                ExpandedShortCutBinding.Control => preferJoystick
                    ? Manager.ui.GetShortCutString(_bindingTerms[3], true)
                    : Manager.ui.GetShortCutString(_bindingTerms[4], false),
                _ => ""
            };
            if (optionalShortCut != ShortCutBinding.none && !string.IsNullOrEmpty(shortCutString))
                return new List<TextAndFormatFields>
                {
                    new()
                    {
                        text = optionalHoverDesc.mTerm,
                        paddingBeneath = 0.125f
                    },
                    new()
                    {
                        text = PugText.ProcessText(string.IsNullOrEmpty(shortCutString2) ? "ShortCutPC" : "SwapHotbarShortCutPC", null, false, false),
                        color = Color.white * 0.95f,
                        dontLocalize = false,
                        formatFields = string.IsNullOrEmpty(shortCutString2) ? new[] { shortCutString } : new[] { shortCutString2, shortCutString },
                        dontLocalizeFormatFields = true
                    }
                };
            return new List<TextAndFormatFields>
            {
                new()
                {
                    text = optionalHoverDesc.mTerm,
                    color = Color.white * 0.99f
                }
            };
        }

        public enum ExpandedShortCutBinding
        {
            None,
            Shift,
            Control,
        }
    }
}