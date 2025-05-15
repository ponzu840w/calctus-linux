using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Shapoco.Platforms.Common;
using Shapoco.Calctus.UI;

namespace Shapoco.Platforms.Linux {
  class LinuxEwmhWindowPopupToggle : IWindowPopupToggle {
    readonly MainForm _mainForm;
    readonly NotifyIcon _notifyIcon;
    readonly IntPtr _display;
    readonly IntPtr _root;
    readonly IntPtr _atomActiveWindow;

    const int ClientMessage = 33;
    const long AnyPropertyType = 0;
    //static readonly IntPtr SubstructureRedirectMask = (IntPtr)(1 << 20);
    //static readonly IntPtr SubstructureNotifyMask   = (IntPtr)(1 << 19);
    const long SubstructureRedirectMask = 1L << 20;
    const long SubstructureNotifyMask   = 1L << 19;

    [DllImport("libX11")]
    static extern int XFetchName(IntPtr display, IntPtr w, out IntPtr name);

    [DllImport("libX11")]
    static extern IntPtr XOpenDisplay(string display_name);

    [DllImport("libX11")]
    static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11")]
    static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11")]
    static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

    [DllImport("libX11")]
    static extern int XGetWindowProperty(
        IntPtr display,
        IntPtr w,
        IntPtr property,
        long long_offset,
        long long_length,
        bool delete,
        IntPtr req_type,
        out IntPtr actual_type,
        out int actual_format,
        out IntPtr nitems,
        out IntPtr bytes_after,
        out IntPtr prop
        );

    [DllImport("libX11")]
    static extern int XFree(IntPtr data);

    [DllImport("libX11")]
    static extern int XIconifyWindow(IntPtr display, IntPtr w, int screen_number);

    [DllImport("libX11")]
    static extern int XSendEvent(
        IntPtr display,
        IntPtr w,
        bool propagate,
        //IntPtr event_mask,
        long event_mask,
        ref XClientMessageEvent evt
        );

    [DllImport("libX11")]
    static extern int XDefaultScreen(IntPtr display);

    [DllImport("libX11")]
    static extern int XQueryTree(
      IntPtr display,
      IntPtr w,
      out IntPtr root_return,
      out IntPtr parent_return,
      out IntPtr children_return,
      out IntPtr nchildren_return
      );

    [StructLayout(LayoutKind.Sequential)]
    struct XClientMessageEvent {
      public int type;
      public IntPtr serial;
      public bool send_event;
      public IntPtr display;
      public IntPtr window;
      public IntPtr message_type;
      public int format;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
      public IntPtr[] data;
    }

    public LinuxEwmhWindowPopupToggle(MainForm mainForm, NotifyIcon notifyIcon) {
      _mainForm         = mainForm;
      _notifyIcon       = notifyIcon;
      _display          = XOpenDisplay(null);
      _root             = XDefaultRootWindow(_display);
      _atomActiveWindow = XInternAtom(_display, "_NET_ACTIVE_WINDOW", false);
    }

    // アプリのトップのウィンドウを得る
    // ツリーを遡って初めて名前付きのウィンドウがあればそれがアプリのトップ
    IntPtr GetAppWindow(IntPtr win) {
      const int MaxDepth = 50;
      for (int depth = 0; depth < MaxDepth && win != IntPtr.Zero; depth++) {
        IntPtr root, parent, children;
        IntPtr nchildren;
        var ok = XQueryTree(_display, win, out root, out parent, out children, out nchildren);
        if (ok == 0) break;             // 親取得失敗
        if (parent == root) return win; // ルート到達
        if (XFetchName(_display, win, out var namePtr) > 0 && namePtr != IntPtr.Zero) {
          XFree(namePtr);
          return win;
        }                               // 名前付き=アプリのトップ
        win = parent;
      }
      return win;
    }

    public void Toggle(object sender, EventArgs e) {
      Console.WriteLine("Toggle called");
      var activeWin = GetActiveWindow();
      var appWin    = GetAppWindow(_mainForm.Handle);
      Console.WriteLine($"[DBG] appWin = 0x{appWin.ToInt64():X}");
      Console.WriteLine($"[DBG] _NET_ACTIVE_WINDOW = 0x{activeWin.ToInt64():X}");
      if (activeWin == appWin) {
        Console.WriteLine("Active window is main form");
        // 自分がアクティブなら隠す／最小化
        if (_notifyIcon.Visible) {
          Console.WriteLine("Hiding main form");
          _mainForm.Visible = false;
        } else {
          Console.WriteLine("Showing main form");
          XIconifyWindow(_display, _mainForm.Handle, XDefaultScreen(_display));
        }
      }
      else {
        Console.WriteLine("Active window is not main form");
        // 別ウィンドウがアクティブなら前面化
        ActivateWindow(_mainForm.Handle);
      }
    }

    IntPtr GetActiveWindow() {
      IntPtr actualType, data;
      int actualFormat;
      IntPtr nitems, bytesAfter;
      var status = XGetWindowProperty(
          _display,
          _root,
          _atomActiveWindow,
          0,
          ~0L,
          false,
          (IntPtr)AnyPropertyType,
          out actualType,
          out actualFormat,
          out nitems,
          out bytesAfter,
          out data
          );
      if (status != 0 || data == IntPtr.Zero) return IntPtr.Zero;
      var win = Marshal.ReadIntPtr(data);
      XFree(data);
      return win;
    }

    void ActivateWindow(IntPtr win) {
      var evt = new XClientMessageEvent {
        type         = ClientMessage,
                     send_event   = true,
                     display      = _display,
                     window       = win,
                     message_type = _atomActiveWindow,
                     format       = 32,
                     data         = new[] {
                       (IntPtr)1, // Source: application
                       IntPtr.Zero,
                       IntPtr.Zero,
                       IntPtr.Zero,
                       IntPtr.Zero
                     }
      };
      XSendEvent(
          _display,
          _root,
          false,
          SubstructureRedirectMask | SubstructureNotifyMask,
          ref evt
          );
    }

    ~LinuxEwmhWindowPopupToggle() {
      if (_display != IntPtr.Zero) {
        XCloseDisplay(_display);
      }
    }
  }
}
