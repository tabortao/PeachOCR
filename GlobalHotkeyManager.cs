using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PeachOCR
{
    public class GlobalHotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        private const int HOTKEY_ID_SCREENSHOT = 9000;

        private IntPtr _windowHandle;
        private HwndSource? _source;
        private bool _isRegistered = false;

        public event EventHandler? ScreenshotHotkeyPressed;

        public void Register(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.Handle;

            if (_windowHandle == IntPtr.Zero)
            {
                helper.EnsureHandle();
                _windowHandle = helper.Handle;
            }

            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);

            string hotkey = Properties.Settings.Default.ScreenshotHotkey;
            if (!string.IsNullOrWhiteSpace(hotkey))
            {
                RegisterHotkeyFromString(hotkey);
            }
        }

        public void RegisterHotkeyFromString(string hotkeyString)
        {
            if (_windowHandle == IntPtr.Zero || _isRegistered)
                return;

            if (string.IsNullOrWhiteSpace(hotkeyString))
                return;

            UnregisterCurrentHotkey();

            var parts = hotkeyString.Split('+');
            if (parts.Length < 2)
                return;

            uint modifiers = 0;
            uint key = 0;

            foreach (var part in parts)
            {
                var trimmedPart = part.Trim().ToLower();
                switch (trimmedPart)
                {
                    case "ctrl":
                    case "control":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "alt":
                        modifiers |= MOD_ALT;
                        break;
                    case "shift":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "win":
                    case "windows":
                        modifiers |= MOD_WIN;
                        break;
                    default:
                        key = GetVirtualKeyCode(trimmedPart);
                        break;
                }
            }

            if (key != 0 && modifiers != 0)
            {
                modifiers |= MOD_NOREPEAT;
                _isRegistered = RegisterHotKey(_windowHandle, HOTKEY_ID_SCREENSHOT, modifiers, key);
            }
        }

        private uint GetVirtualKeyCode(string keyString)
        {
            if (keyString.Length == 1)
            {
                char c = keyString[0];
                if (c >= 'a' && c <= 'z')
                    return (uint)char.ToUpper(c);
                if (c >= '0' && c <= '9')
                    return (uint)c;
            }

            switch (keyString.ToUpper())
            {
                case "A": return 0x41;
                case "B": return 0x42;
                case "C": return 0x43;
                case "D": return 0x44;
                case "E": return 0x45;
                case "F": return 0x46;
                case "G": return 0x47;
                case "H": return 0x48;
                case "I": return 0x49;
                case "J": return 0x4A;
                case "K": return 0x4B;
                case "L": return 0x4C;
                case "M": return 0x4D;
                case "N": return 0x4E;
                case "O": return 0x4F;
                case "P": return 0x50;
                case "Q": return 0x51;
                case "R": return 0x52;
                case "S": return 0x53;
                case "T": return 0x54;
                case "U": return 0x55;
                case "V": return 0x56;
                case "W": return 0x57;
                case "X": return 0x58;
                case "Y": return 0x59;
                case "Z": return 0x5A;
                case "F1": return 0x70;
                case "F2": return 0x71;
                case "F3": return 0x72;
                case "F4": return 0x73;
                case "F5": return 0x74;
                case "F6": return 0x75;
                case "F7": return 0x76;
                case "F8": return 0x77;
                case "F9": return 0x78;
                case "F10": return 0x79;
                case "F11": return 0x7A;
                case "F12": return 0x7B;
                default: return 0;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();

                if (hotkeyId == HOTKEY_ID_SCREENSHOT)
                {
                    ScreenshotHotkeyPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void UnregisterCurrentHotkey()
        {
            if (_windowHandle != IntPtr.Zero && _isRegistered)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID_SCREENSHOT);
                _isRegistered = false;
            }
        }

        public void Dispose()
        {
            UnregisterCurrentHotkey();
            _source?.RemoveHook(HwndHook);
            _source = null;
        }
    }
}
