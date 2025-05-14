using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Shapoco.Calctus;
using Shapoco.Calctus.UI;

namespace Shapoco.Platforms.Common {
    internal partial class HotKeyManager {
        readonly IHotKeyService _service;
        readonly MainForm _mainForm;
        readonly NotifyIcon _notifyIcon;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public HotKeyManager(MainForm mf, NotifyIcon notifyIcon = null) {
            _mainForm = mf; _notifyIcon = notifyIcon;
            if (Platform.IsWindows()) {
                _service = new Windows.WindowsHotKeyService();
            } else if (Platform.IsLinuxMono()) {
                _service = new Linux.LinuxHotKeyService();
            } else {
                throw new PlatformNotSupportedException();
            }
            _service.HotKeyPressed += OnHotKey;
        }

        public void Enable() {
            var s = Settings.Instance;
            if (!s.Hotkey_Enabled) return;
            if (!_service.Register(
                  (ModifierKey)(
                    (s.HotKey_Ctrl  ? ModifierKey.Ctrl  : 0) |
                    (s.HotKey_Alt   ? ModifierKey.Alt   : 0) |
                    (s.HotKey_Shift ? ModifierKey.Shift : 0) |
                    (s.HotKey_Win   ? ModifierKey.Win   : 0)),
                  s.HotKey_KeyCode))
                MessageBox.Show("Hotkey registration failed.");
        }

        public void Disable() => _service.Unregister();

        private void OnHotKey(object sender, EventArgs e)
        {
            if (GetForegroundWindow() == _mainForm.Handle)
            {
                if (_notifyIcon.Visible)
                {
                    _mainForm.Visible = false;
                }
                else
                {
                    _mainForm.WindowState = FormWindowState.Minimized;
                }
            }
            else
            {
                _mainForm.ShowForeground();
            }
        }
    }

    [Flags]
    public enum ModifierKey {
        None = 0, Alt = 1, Ctrl = 2, Shift = 4, Win = 8
    }

}
