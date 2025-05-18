using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Shapoco.Platforms.Linux.X11 {
  /// <summary>
  /// 参照カウントで単一の X Display handle を管理する
  /// XOpenDisplay はプログラム全体で一度だけ呼ばれる
  /// </summary>
  public sealed class X11DisplayManager {
    private static readonly object _sync = new object();
    private static X11DisplayManager _instance;

    private IntPtr _display;
    private int _refCount;

    private X11DisplayManager() {
      _display = XOpenDisplay(null);
      if (_display == IntPtr.Zero)
        Console.WriteLine("[DBG X11HotKey] Failed to open X11 display. Make sure X11/XWayland is running.");
      _refCount = 1;
    }

    /// <summary>
    /// シングルトンなインスタンスを返す
    /// </summary>
    public static X11DisplayManager Instance {
      get {
        lock (_sync) {
          if (_instance == null) {
            _instance = new X11DisplayManager();
          } else {
            _instance._refCount++;
          }
          return _instance;
        }
      }
    }

    /// <summary>
    /// ネイティブの X Display ポインタ
    /// </summary>
    public IntPtr Display => _display;

    /// <summary>
    /// 参照を開放する 誰も参照しなくなったら X Display を閉じる
    /// </summary>
    public void Release() {
      lock (_sync) {
        if (_instance == null) return;
        _instance._refCount--;
        if (_instance._refCount <= 0) {
          XCloseDisplay(_instance._display);
          _instance._display = IntPtr.Zero;
          _instance = null;
        }
      }
    }

    [DllImport("libX11")]
    private static extern IntPtr XOpenDisplay(string display);

    [DllImport("libX11")]
    private static extern int XCloseDisplay(IntPtr display);
  }
}
