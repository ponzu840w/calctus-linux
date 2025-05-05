using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Shapoco.Platforms;

namespace Shapoco.Platforms.Common {
  /// <summary>
  /// DPI を取得するためのヘルパー
  /// Windowsなら DeviceDpi 、それ以外なら DpiX を使う。
  /// 全部 DpiX で良い気もする
  /// <\summary>
  public static class DpiHelper {
    // Control->int デリゲートをキャッシュ
    private static readonly Func<Control, int> deviceDpiAccessor;

    static DpiHelper() {
      if (Platform.IsWindows()) {
        // Windows 上なら DeviceDpi プロパティをリフレクションで拾う
        var prop = typeof(Control).GetProperty(
                     "DeviceDpi",
                     BindingFlags.Instance | BindingFlags.Public
                   );

        if (prop?.CanRead == true) {
          // インスタンス プロパティの getter をオープン デリゲート化
          var getter = prop.GetGetMethod();
          deviceDpiAccessor = (Func<Control, int>)Delegate.CreateDelegate(
                                typeof(Func<Control, int>),
                                getter
                              );
        }

      }

      // 上記で組み立てられなかった（Mono など）ならフォールバック版をセット
      if (deviceDpiAccessor == null) {
        deviceDpiAccessor = ctrl => {
          using (var g = ctrl.CreateGraphics())
            return (int)g.DpiX;
        };
      }

    }

    /// <summary>
    /// Control.DeviceDpi が使えるならそれで、使えない環境では CreateGraphics().DpiX で DPI を返す。
    /// </summary>
    public static int GetDeviceDpi(Control ctrl) {
        return deviceDpiAccessor(ctrl);
    }

  }
}
