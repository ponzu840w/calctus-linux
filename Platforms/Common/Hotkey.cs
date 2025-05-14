// C#|グローバルホットキーを登録する – 貧脚レーサーのサボり日記
// https://anis774.net/codevault/hotkey.html

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Shapoco.Calctus;
using Shapoco.Calctus.UI;

namespace Shapoco.Platforms.Common {
    internal partial class HotKeyManager {
        private HotKey _hotkey = null;
        private MainForm _mainForm = null;
        private NotifyIcon _notifyIcon = null;

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public HotKeyManager(MainForm mf, NotifyIcon notifyIcon = null) {
            _mainForm = mf;
            _notifyIcon = notifyIcon;
        }

        public void enableHotkey()
        {
            var s = Settings.Instance;
            if (s.Hotkey_Enabled && s.HotKey_KeyCode != Keys.None)
            {
                MOD_KEY mod = MOD_KEY.NONE;
                if (s.HotKey_Win) mod |= MOD_KEY.WIN;
                if (s.HotKey_Alt) mod |= MOD_KEY.ALT;
                if (s.HotKey_Ctrl) mod |= MOD_KEY.CONTROL;
                if (s.HotKey_Shift) mod |= MOD_KEY.SHIFT;
                _hotkey = new HotKey(mod, s.HotKey_KeyCode);
                _hotkey.HotKeyPush += _hotkey_HotKeyPush;
            }
        }

        public void disableHotkey()
        {
            if (_hotkey != null)
            {
                _hotkey.HotKeyPush -= _hotkey_HotKeyPush;
                _hotkey.Dispose();
                _hotkey = null;
            }
        }

        public void _hotkey_HotKeyPush(object sender, EventArgs e)
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

    /// <summary>
    /// グローバルホットキーを登録するクラス。
    /// 使用後は必ずDisposeすること。
    /// </summary>
    public class HotKey : IDisposable {
        HotKeyForm form;
        /// <summary>
        /// ホットキーが押されると発生する。
        /// </summary>
        public event EventHandler HotKeyPush;

        /// <summary>
        /// ホットキーを指定して初期化する。
        /// 使用後は必ずDisposeすること。
        /// </summary>
        /// <param name="modKey">修飾キー</param>
        /// <param name="key">キー</param>
        public HotKey(MOD_KEY modKey, Keys key) {
            form = new HotKeyForm(modKey, key, raiseHotKeyPush);
        }

        private void raiseHotKeyPush() {
            if (HotKeyPush != null) {
                HotKeyPush(this, EventArgs.Empty);
            }
        }

        public void Dispose() {
            form.Dispose();
        }

        private class HotKeyForm : Form {
            [DllImport("user32.dll")]
            extern static int RegisterHotKey(IntPtr HWnd, int ID, MOD_KEY MOD_KEY, Keys KEY);

            [DllImport("user32.dll")]
            extern static int UnregisterHotKey(IntPtr HWnd, int ID);

            const int WM_HOTKEY = 0x0312;
            int id;
            ThreadStart proc;

            public HotKeyForm(MOD_KEY modKey, Keys key, ThreadStart proc) {
                this.proc = proc;
                bool success = false;
                for (int i = 0x0000; i <= 0xbfff; i++) {
                    if (RegisterHotKey(this.Handle, i, modKey, key) != 0) {
                        id = i;
                        success = true;
                        break;
                    }
                }
                if (!success) {
                    //MessageBox.Show("Hotkey register failed.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine("Hotkey register failed.");
                }
            }

            protected override void WndProc(ref Message m) {
                base.WndProc(ref m);

                if (m.Msg == WM_HOTKEY) {
                    if ((int)m.WParam == id) {
                        proc();
                    }
                }
            }

            protected override void Dispose(bool disposing) {
                UnregisterHotKey(this.Handle, id);
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// HotKeyクラスの初期化時に指定する修飾キー
    /// </summary>
    public enum MOD_KEY : int {
        NONE = 0x0000,
        ALT = 0x0001,
        CONTROL = 0x0002,
        SHIFT = 0x0004,
        WIN = 0x0008,
    }
}
