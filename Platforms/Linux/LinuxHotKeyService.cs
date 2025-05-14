using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;  // Keys 型のために
using Shapoco.Platforms.Common;

namespace Shapoco.Platforms.Linux {
  public class LinuxHotKeyService : IHotKeyService
  {
    // X11 への接続ハンドルとルートウィンドウ
    private IntPtr _display = IntPtr.Zero;
    private IntPtr _rootWindow = IntPtr.Zero;

    // 登録済みキー情報
    private int    _keycode;
    private uint   _modifiers;
    private Thread _evtThread;
    private bool   _running;

    public event EventHandler HotKeyPressed;

    public bool Register(ModifierKey mods, Keys key)
    {
      // １．X サーバーへ接続
      _display = XOpenDisplay(IntPtr.Zero);
      if (_display == IntPtr.Zero) return false;

      // ２．ルートウィンドウを取得
      _rootWindow = XDefaultRootWindow(_display);

      // ３．Keys → X KeySym → KeyCode
      var sym      = (KeySym)KeyInterop.VirtualKeyFromKey(key);
      _keycode     = XKeysymToKeycode(_display, sym);
      _modifiers   = ConvertModifiers(mods);

      // ４．ホットキーをグラブ
      XGrabKey(_display,
          _keycode,
          _modifiers,
          _rootWindow,
          true,   // owner_events
          GrabModeAsync,
          GrabModeAsync);

      // ５．KeyPress イベントを受け取るように設定
      XSelectInput(_display, _rootWindow, EventMask.KeyPressMask);

      // ６．イベントループを別スレッドで開始
      _running   = true;
      _evtThread = new Thread(EventLoop) { IsBackground = true };
      _evtThread.Start();

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

    // ---------------- private ----------------

    private void EventLoop()
    {
      while (_running)
      {
        XEvent ev;
        XNextEvent(_display, out ev);
        if (ev.type == EventKeyPress)
          HotKeyPressed?.Invoke(this, EventArgs.Empty);
      }
    }

    private static uint ConvertModifiers(ModifierKey mods)
    {
      uint m = 0;
      if (mods.HasFlag(ModifierKey.Ctrl))  m |= ControlMask;
      if (mods.HasFlag(ModifierKey.Alt))   m |= Mod1Mask;
      if (mods.HasFlag(ModifierKey.Shift)) m |= ShiftMask;
      // ※ Win キーを取りたいなら Mod4Mask を追加
      return m;
    }

#region P/Invoke for X11

    private const int GrabModeAsync     = 1;
    private const int EventMaskKeyPress = 1 << 0;
    private const int EventKeyPress     = 2;

    private const uint ControlMask = (1u << 2);
    private const uint Mod1Mask    = (1u << 3);
    private const uint ShiftMask   = (1u << 0);
    private const uint Mod4Mask    = (1u << 6);  // Super (Win)

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
          KeySym keysym);

    // XEvent の最小定義
    [StructLayout(LayoutKind.Explicit)]
      private struct XEvent
      {
        [FieldOffset(0)] public int type;
        // 他のフィールドは省略
      }

    private enum KeySym : ulong { /* 必要な KeySym 定義を追加 */ }

#endregion
  }
}
