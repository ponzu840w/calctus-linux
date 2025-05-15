using System;
using System.Windows.Forms;
using Shapoco.Calctus;
using Shapoco.Calctus.UI;

namespace Shapoco.Platforms.Common {
  internal partial class HotKeyManager {
    readonly IHotKeyService _service;    // ホットキーの登録・解除（マルチプラットフォーム）
    readonly IWindowPopupToggle _winpop; // ウィンドウの出没（マルチプラットフォーム）

    // プラットフォーム依存部分はコンストラクタで実装を注入
    public HotKeyManager(MainForm mf, NotifyIcon notifyIcon = null) {
      if (Platform.IsWindows()) {
        _service = new Windows.WindowsHotKeyService();
        _winpop = new Windows.WindowsWindowPopupToggle(mf, notifyIcon);
        _service.HotKeyPressed += _winpop.Toggle;
      } else if (Platform.IsLinuxMono()) {
        _service = new Linux.LinuxX11HotKeyService();
        _winpop = new Linux.LinuxEwmhWindowPopupToggle(mf, notifyIcon);
        _service.HotKeyPressed += _winpop.Toggle;
      } else {
        throw new PlatformNotSupportedException();
      }
    }

    // 設定を読んでホットキーを登録する
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

    // ホットキーを解除する
    public void Disable() => _service.Unregister();
  }

  [Flags]
  public enum ModifierKey {
    None = 0, Alt = 1, Ctrl = 2, Shift = 4, Win = 8
  }
}
