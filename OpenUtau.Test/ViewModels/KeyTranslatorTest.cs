using System.Linq;
using Avalonia.Input;
using Xunit;

namespace OpenUtau.App.ViewModels {
    public class KeyTranslatorTest {
        // ==================== IsKeyMatch ====================

        [Fact]
        public void IsKeyMatch_SameKey_ReturnsTrue() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.A, Key.A));
            Assert.True(KeyTranslator.IsKeyMatch(Key.D1, Key.D1));
            Assert.True(KeyTranslator.IsKeyMatch(Key.Space, Key.Space));
        }

        [Fact]
        public void IsKeyMatch_OemPipe_MatchesOem5AndOemBackslash() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemPipe, Key.Oem5));
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemPipe, Key.OemBackslash));
        }

        [Fact]
        public void IsKeyMatch_OemOpenBrackets_MatchesOem4() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemOpenBrackets, Key.Oem4));
        }

        [Fact]
        public void IsKeyMatch_OemCloseBrackets_MatchesOem6() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemCloseBrackets, Key.Oem6));
        }

        [Fact]
        public void IsKeyMatch_OemQuotes_MatchesOem7() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemQuotes, Key.Oem7));
        }

        [Fact]
        public void IsKeyMatch_OemSemicolon_MatchesOem1() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemSemicolon, Key.Oem1));
        }

        [Fact]
        public void IsKeyMatch_OemTilde_MatchesOem3AndOem8() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemTilde, Key.Oem3));
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemTilde, Key.Oem8));
        }

        [Fact]
        public void IsKeyMatch_OemMinus_MatchesSubtract() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemMinus, Key.Subtract));
        }

        [Fact]
        public void IsKeyMatch_OemPlus_MatchesAdd() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemPlus, Key.Add));
        }

        [Fact]
        public void IsKeyMatch_OemQuestion_MatchesOem2() {
            Assert.True(KeyTranslator.IsKeyMatch(Key.OemQuestion, Key.Oem2));
        }

        [Fact]
        public void IsKeyMatch_DifferentKeys_ReturnsFalse() {
            Assert.False(KeyTranslator.IsKeyMatch(Key.A, Key.B));
            Assert.False(KeyTranslator.IsKeyMatch(Key.OemPipe, Key.A));
            Assert.False(KeyTranslator.IsKeyMatch(Key.OemTilde, Key.Space));
        }

        // ==================== GetFriendlyName(string) ====================

        [Fact]
        public void GetFriendlyName_DigitKeys_ReturnsNumbers() {
            Assert.Equal("1", KeyTranslator.GetFriendlyName("D1"));
            Assert.Equal("2", KeyTranslator.GetFriendlyName("D2"));
            Assert.Equal("3", KeyTranslator.GetFriendlyName("D3"));
            Assert.Equal("4", KeyTranslator.GetFriendlyName("D4"));
            Assert.Equal("5", KeyTranslator.GetFriendlyName("D5"));
            Assert.Equal("6", KeyTranslator.GetFriendlyName("D6"));
            Assert.Equal("7", KeyTranslator.GetFriendlyName("D7"));
            Assert.Equal("8", KeyTranslator.GetFriendlyName("D8"));
            Assert.Equal("9", KeyTranslator.GetFriendlyName("D9"));
            Assert.Equal("0", KeyTranslator.GetFriendlyName("D0"));
        }

        [Fact]
        public void GetFriendlyName_NavigationKeys_ReturnsFriendlyNames() {
            Assert.Equal("Esc", KeyTranslator.GetFriendlyName("Escape"));
            Assert.Equal("Enter", KeyTranslator.GetFriendlyName("Return"));
            Assert.Equal("Backspace", KeyTranslator.GetFriendlyName("Back"));
            Assert.Equal("Ins", KeyTranslator.GetFriendlyName("Insert"));
            Assert.Equal("PgUp", KeyTranslator.GetFriendlyName("PageUp"));
            Assert.Equal("PgDn", KeyTranslator.GetFriendlyName("PageDown"));
        }

        [Fact]
        public void GetFriendlyName_NumpadKeys_ReturnsFriendlyNames() {
            Assert.Equal("(Num /)", KeyTranslator.GetFriendlyName("Divide"));
            Assert.Equal("(Num *)", KeyTranslator.GetFriendlyName("Multiply"));
            Assert.Equal("(Num -)", KeyTranslator.GetFriendlyName("Subtract"));
            Assert.Equal("(Num +)", KeyTranslator.GetFriendlyName("Add"));
            Assert.Equal("(Num .)", KeyTranslator.GetFriendlyName("Decimal"));
            Assert.Equal("Num 0", KeyTranslator.GetFriendlyName("NumPad0"));
            Assert.Equal("Num 9", KeyTranslator.GetFriendlyName("NumPad9"));
        }

        [Fact]
        public void GetFriendlyName_OemKeys_ReturnsSymbols() {
            Assert.Equal("~", KeyTranslator.GetFriendlyName("OemTilde"));
            Assert.Equal("-", KeyTranslator.GetFriendlyName("OemMinus"));
            Assert.Equal("=", KeyTranslator.GetFriendlyName("OemPlus"));
            Assert.Equal("[", KeyTranslator.GetFriendlyName("OemOpenBrackets"));
            Assert.Equal("]", KeyTranslator.GetFriendlyName("OemCloseBrackets"));
            Assert.Equal("\\", KeyTranslator.GetFriendlyName("OemPipe"));
            Assert.Equal(";", KeyTranslator.GetFriendlyName("OemSemicolon"));
            Assert.Equal("'", KeyTranslator.GetFriendlyName("OemQuotes"));
            Assert.Equal(",", KeyTranslator.GetFriendlyName("OemComma"));
            Assert.Equal(".", KeyTranslator.GetFriendlyName("OemPeriod"));
            Assert.Equal("/", KeyTranslator.GetFriendlyName("OemQuestion"));
        }

        [Fact]
        public void GetFriendlyName_OemAliases_ReturnsSameSymbols() {
            Assert.Equal("~", KeyTranslator.GetFriendlyName("Oem3"));
            Assert.Equal("~", KeyTranslator.GetFriendlyName("Oem8"));
            Assert.Equal("-", KeyTranslator.GetFriendlyName("OemMinusSign"));
            Assert.Equal("=", KeyTranslator.GetFriendlyName("OemPlusSign"));
            Assert.Equal("[", KeyTranslator.GetFriendlyName("Oem4"));
            Assert.Equal("]", KeyTranslator.GetFriendlyName("Oem6"));
            Assert.Equal("\\", KeyTranslator.GetFriendlyName("Oem5"));
            Assert.Equal("\\", KeyTranslator.GetFriendlyName("OemBackslash"));
            Assert.Equal(";", KeyTranslator.GetFriendlyName("Oem1"));
            Assert.Equal("'", KeyTranslator.GetFriendlyName("Oem7"));
            Assert.Equal(",", KeyTranslator.GetFriendlyName("OemCommaSign"));
            Assert.Equal(".", KeyTranslator.GetFriendlyName("OemPeriodSign"));
            Assert.Equal("/", KeyTranslator.GetFriendlyName("Oem2"));
        }

        [Fact]
        public void GetFriendlyName_UnknownKey_ReturnsOriginal() {
            Assert.Equal("F24", KeyTranslator.GetFriendlyName("F24"));
            Assert.Equal("BrowserBack", KeyTranslator.GetFriendlyName("BrowserBack"));
        }

        // ==================== GetFriendlyModifiersName ====================

        [Fact]
        public void GetFriendlyModifiersName_None_ReturnsEmpty() {
            Assert.Equal(string.Empty, KeyTranslator.GetFriendlyModifiersName(KeyModifiers.None));
        }

        [Fact]
        public void GetFriendlyModifiersName_Control_ReturnsCtrl() {
            var result = KeyTranslator.GetFriendlyModifiersName(KeyModifiers.Control);
            // On non-Mac: "Ctrl"; on Mac: "⌃"
            Assert.True(result.Contains("Ctrl") || result.Contains("⌃"));
        }

        [Fact]
        public void GetFriendlyModifiersName_Combined_ReturnsJoined() {
            var result = KeyTranslator.GetFriendlyModifiersName(KeyModifiers.Control | KeyModifiers.Shift);
            // On non-Mac: "Ctrl + Shift"; on Mac: "⌃ ⇧"
            Assert.True(result.Contains("Ctrl") || result.Contains("⌃"));
            Assert.True(result.Contains("Shift") || result.Contains("⇧"));
        }

        // ==================== StringToGesture ====================

        [Fact]
        public void StringToGesture_SimpleKey_ReturnsGesture() {
            var gesture = KeyTranslator.StringToGesture("Space");
            Assert.Equal(Key.Space, gesture.Key);
            Assert.Equal(KeyModifiers.None, gesture.KeyModifiers);
        }

        [Fact]
        public void StringToGesture_WithModifier_ReturnsGesture() {
            var gesture = KeyTranslator.StringToGesture("Ctrl+A");
            Assert.Equal(Key.A, gesture.Key);
            Assert.True(gesture.KeyModifiers.HasFlag(KeyModifiers.Control));
        }

        [Fact]
        public void StringToGesture_MultipleModifiers_ReturnsGesture() {
            var gesture = KeyTranslator.StringToGesture("Ctrl+Shift+A");
            Assert.Equal(Key.A, gesture.Key);
            Assert.True(gesture.KeyModifiers.HasFlag(KeyModifiers.Control));
            Assert.True(gesture.KeyModifiers.HasFlag(KeyModifiers.Shift));
        }

        [Fact]
        public void StringToGesture_InvalidString_ReturnsKeyNone() {
            var gesture = KeyTranslator.StringToGesture("!invalid!!");
            Assert.Equal(Key.None, gesture.Key);
        }

        [Fact]
        public void StringToGesture_EmptyString_ReturnsKeyNone() {
            var gesture = KeyTranslator.StringToGesture("");
            Assert.Equal(Key.None, gesture.Key);
        }

        // ==================== GestureToString roundtrip ====================

        [Fact]
        public void GestureToString_Roundtrip_Simple() {
            var original = KeyTranslator.StringToGesture("Space");
            var str = KeyTranslator.GestureToString(original);
            var restored = KeyTranslator.StringToGesture(str);
            Assert.Equal(original.Key, restored.Key);
            Assert.Equal(original.KeyModifiers, restored.KeyModifiers);
        }

        [Fact]
        public void GestureToString_Roundtrip_WithModifiers() {
            var original = KeyTranslator.StringToGesture("Ctrl+Shift+V");
            var str = KeyTranslator.GestureToString(original);
            var restored = KeyTranslator.StringToGesture(str);
            Assert.Equal(original.Key, restored.Key);
            Assert.Equal(original.KeyModifiers, restored.KeyModifiers);
        }

        // ==================== GetFriendlyName(Key, KeyModifiers) ====================

        [Fact]
        public void GetFriendlyName_KeyAndModifiers_NoMods() {
            var name = KeyTranslator.GetFriendlyName(Key.Space, KeyModifiers.None);
            Assert.Equal("Space", name);
        }

        [Fact]
        public void GetFriendlyName_KeyAndModifiers_WithCtrl() {
            var name = KeyTranslator.GetFriendlyName(Key.A, KeyModifiers.Control);
            // On non-Mac: "Ctrl + A"; on Mac: "⌃ A"
            Assert.True(name.Contains("Ctrl") || name.Contains("⌃"));
            Assert.Contains("A", name);
        }

        // ==================== DefShortcuts ====================

        [Fact]
        public void DefShortcuts_HasEssentialShortcuts() {
            var ids = KeyTranslator.DefShortcuts.Select(s => s.ActionId).ToHashSet();
            Assert.Contains("menu.edit.undo", ids);
            Assert.Contains("menu.edit.redo", ids);
            Assert.Contains("menu.edit.copy", ids);
            Assert.Contains("menu.edit.paste", ids);
            Assert.Contains("PlayOrPause", ids);
            Assert.Contains("SaveProject", ids);
            Assert.Contains("SelectAll", ids);
        }

        [Fact]
        public void DefShortcuts_AllParseable() {
            foreach (var binding in KeyTranslator.DefShortcuts) {
                foreach (var shortcut in binding.Shortcuts) {
                    var gesture = KeyTranslator.StringToGesture(shortcut);
                    Assert.NotEqual(Key.None, gesture.Key);
                }
            }
        }

        [Fact]
        public void DefShortcuts_NoDuplicateActionIds() {
            var ids = KeyTranslator.DefShortcuts.Select(s => s.ActionId).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void DefShortcuts_RedoHasTwoBindings() {
            var redo = KeyTranslator.DefShortcuts.First(s => s.ActionId == "menu.edit.redo");
            Assert.Equal(2, redo.Shortcuts.Length);
            Assert.Contains("Ctrl+Y", redo.Shortcuts);
            Assert.Contains("Ctrl+Shift+Z", redo.Shortcuts);
        }
    }
}
