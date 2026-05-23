using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using OpenUtau.Core.Util;

namespace OpenUtau.App.ViewModels {
    public static class KeyTranslator {
        public static readonly bool IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Normalizes modifiers so Windows "Ctrl" becomes Mac "Cmd", 
        /// and Windows "Win" becomes Mac "Ctrl".
        /// </summary>
        private static KeyModifiers NormalizeModifiers(KeyModifiers modifiers) {
            if (!IsMac) return modifiers;

            var normalized = modifiers;
            bool hasCtrl = modifiers.HasFlag(KeyModifiers.Control);
            bool hasMeta = modifiers.HasFlag(KeyModifiers.Meta);

            if (hasCtrl) {
                normalized &= ~KeyModifiers.Control;
                normalized |= KeyModifiers.Meta; 
            }
            if (hasMeta) {
                normalized &= ~KeyModifiers.Meta;
                normalized |= KeyModifiers.Control; 
            }

            return normalized;
        }

        public static string GetFriendlyName(string keyName) {
            return keyName switch {
                // Modifiers
                "Windows" or "LWin" or "RWin" => IsMac ? "⌘" : "Win",
                "LeftAlt" or "RightAlt" or "Alt" => IsMac ? "⌥" : "Alt",
                "Control" or "LeftCtrl" or "RightCtrl" or "LControl" or "RControl" => IsMac ? "⌃" : "Ctrl", 
                "Shift" or "LeftShift" or "RightShift" => IsMac ? "⇧" : "Shift",
                
                // Navigation & Editing
                "Escape" => "Esc",
                "Return" => "Enter",
                "Back" => IsMac ? "Delete" : "Backspace",
                "Delete" => IsMac ? "Forward Del" : "Del",
                "Insert" => "Ins",
                "PageUp" => "PgUp",
                "PageDown" => "PgDn",
                "Capital" => "Caps Lock",
                "Scroll" => "Scroll Lock",
                "NumLock" => "Num Lock",
                "Snapshot" => "Print Screen",

                // Numpad
                "Divide" => "(Num /)",
                "Multiply" => "(Num *)",
                "Subtract" => "(Num -)",
                "Add" => "(Num +)",
                "Decimal" => "(Num .)",
                "NumPad0" => "Num 0", "NumPad1" => "Num 1", "NumPad2" => "Num 2",
                "NumPad3" => "Num 3", "NumPad4" => "Num 4", "NumPad5" => "Num 5",
                "NumPad6" => "Num 6", "NumPad7" => "Num 7", "NumPad8" => "Num 8",
                "NumPad9" => "Num 9",

                // Digits
                "D1" => "1", "D2" => "2", "D3" => "3", "D4" => "4", "D5" => "5",
                "D6" => "6", "D7" => "7", "D8" => "8", "D9" => "9", "D0" => "0",

                // OEM Symbols
                "OemTilde" or "Oem8" or "Oem3" => "~",
                "OemMinus" or "OemMinusSign" => "-",
                "OemPlus" or "OemPlusSign" => "=",
                "OemOpenBrackets" or "Oem4" => "[",
                "OemCloseBrackets" or "Oem6" => "]",
                "OemPipe" or "Oem5" or "OemBackslash" => "\\",
                "OemSemicolon" or "Oem1" => ";",
                "OemQuotes" or "Oem7" => "'",
                "OemComma" or "OemCommaSign" => ",",
                "OemPeriod" or "OemPeriodSign" => ".",
                "OemQuestion" or "Oem2" => "/",

                _ => keyName
            };
        }

        public static bool IsKeyMatch(Key savedKey, Key pressedKey) {
            if (savedKey == pressedKey) return true;

            return savedKey switch {
                Key.OemPipe => pressedKey == Key.Oem5 || pressedKey == Key.OemBackslash,
                Key.OemOpenBrackets => pressedKey == Key.Oem4,
                Key.OemCloseBrackets => pressedKey == Key.Oem6,
                Key.OemQuotes => pressedKey == Key.Oem7,
                Key.OemSemicolon => pressedKey == Key.Oem1,
                Key.OemTilde => pressedKey == Key.Oem3 || pressedKey == Key.Oem8,
                Key.OemMinus => pressedKey == Key.Subtract,
                Key.OemPlus => pressedKey == Key.Add,
                Key.OemQuestion => pressedKey == Key.Oem2,
                _ => false
            };
        }

        public static string GetFriendlyModifiersName(KeyModifiers modifiers) {
            if (modifiers == KeyModifiers.None) return "";
            
            // Apply macOS normalization before rendering strings
            modifiers = NormalizeModifiers(modifiers);

            var parts = new System.Collections.Generic.List<string>();
            if (modifiers.HasFlag(KeyModifiers.Control)) {
                parts.Add(IsMac ? "⌃" : "Ctrl");
            }
            if (modifiers.HasFlag(KeyModifiers.Alt)) {
                parts.Add(IsMac ? "⌥" : "Alt");
            }
            if (modifiers.HasFlag(KeyModifiers.Shift)) {
                parts.Add(IsMac ? "⇧" : "Shift");
            }
            if (modifiers.HasFlag(KeyModifiers.Meta)) {
                parts.Add(IsMac ? "⌘" : "Win");
            }

            return string.Join(IsMac ? "" : " + ", parts);
        }

        public static Avalonia.Input.KeyGesture? GetGesture(string actionId) {
            var sc = Preferences.Default.Shortcuts?.FirstOrDefault(s => s.ActionId == actionId);
            
            if (sc != null && 
                Enum.TryParse<Avalonia.Input.Key>(sc.KeyName, out var k) && 
                Enum.TryParse<Avalonia.Input.KeyModifiers>(sc.ModifiersName, out var m) && 
                k != Avalonia.Input.Key.None) {
                
                // Apply macOS normalization to the actual hotkey gesture
                m = NormalizeModifiers(m);

                return new Avalonia.Input.KeyGesture(k, m);
            }
            return null;
        }
    }
}