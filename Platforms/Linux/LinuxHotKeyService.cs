using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Shapoco.Platforms.Common;

namespace Shapoco.Platforms.Linux
{
    /// <summary>
    /// X11 を使ったグローバルホットキー実装
    /// </summary>
    public class LinuxHotKeyService : IHotKeyService
    {
        private IntPtr _display = IntPtr.Zero;
        private IntPtr _rootWindow = IntPtr.Zero;
        private int _keycode;
        private uint _modifiers;
        private Thread _eventThread;
        private bool _running;

        public event EventHandler HotKeyPressed;

        public bool Register(ModifierKey modifiers, Keys key)
        {
            // X サーバーへ接続
            _display = XOpenDisplay(IntPtr.Zero);
            if (_display == IntPtr.Zero)
                return false;

            // ルートウィンドウ取得
            _rootWindow = XDefaultRootWindow(_display);

            // Keys → X KeySym → KeyCode
            // KeySym はキー名称を文字列で指定
            ulong keysym = XStringToKeysym(key.ToString());
            _keycode = XKeysymToKeycode(_display, keysym);
            _modifiers = ConvertModifiers(modifiers);

            // ホットキー登録
            XGrabKey(_display,
                     _keycode,
                     _modifiers,
                     _rootWindow,
                     true,
                     GrabModeAsync,
                     GrabModeAsync);

            // KeyPress イベントを受け取る
            XSelectInput(_display, _rootWindow, KeyPressMask);

            // イベントループ開始
            _running = true;
            _eventThread = new Thread(EventLoop) { IsBackground = true };
            _eventThread.Start();

            return true;
        }

        public void Unregister()
        {
            _running = false;
            if (_display != IntPtr.Zero)
            {
                XUngrabKey(_display, _keycode, _modifiers, _rootWindow);
                XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
        }

        public void Dispose() => Unregister();

        private void EventLoop()
        {
            while (_running)
            {
                XEvent xev;
                XNextEvent(_display, out xev);
                if (xev.type == EventKeyPress)
                    HotKeyPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        private static uint ConvertModifiers(ModifierKey mods)
        {
            uint m = 0;
            if (mods.HasFlag(ModifierKey.Ctrl)) m |= ControlMask;
            if (mods.HasFlag(ModifierKey.Alt)) m |= Mod1Mask;
            if (mods.HasFlag(ModifierKey.Shift)) m |= ShiftMask;
            if (mods.HasFlag(ModifierKey.Win)) m |= Mod4Mask;
            return m;
        }

        #region P/Invoke X11

        private const int GrabModeAsync = 1;
        private const long KeyPressMask = (1L << 0);
        private const int EventKeyPress = 2;

        private const uint ShiftMask = (1u << 0);
        private const uint ControlMask = (1u << 2);
        private const uint Mod1Mask = (1u << 3);
        private const uint Mod4Mask = (1u << 6);

        [DllImport("libX11")]
        private static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11")]
        private static extern int XCloseDisplay(IntPtr display);

        [DllImport("libX11")]
        private static extern IntPtr XDefaultRootWindow(IntPtr display);

        [DllImport("libX11")]
        private static extern void XGrabKey(
            IntPtr display,
            int keycode,
            uint modifiers,
            IntPtr grab_window,
            bool owner_events,
            int pointer_mode,
            int keyboard_mode);

        [DllImport("libX11")]
        private static extern void XUngrabKey(
            IntPtr display,
            int keycode,
            uint modifiers,
            IntPtr grab_window);

        [DllImport("libX11")]
        private static extern int XSelectInput(
            IntPtr display,
            IntPtr window,
            long event_mask);

        [DllImport("libX11")]
        private static extern int XNextEvent(
            IntPtr display,
            out XEvent event_return);

        [DllImport("libX11")]
        private static extern int XKeysymToKeycode(
            IntPtr display,
            ulong keysym);

        [DllImport("libX11")]
        private static extern ulong XStringToKeysym(string str);

        [StructLayout(LayoutKind.Sequential)]
        private struct XEvent
        {
            public int type;
            // 他は不要
        }

        #endregion
    }
}