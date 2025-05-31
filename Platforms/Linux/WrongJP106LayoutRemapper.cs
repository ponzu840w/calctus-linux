using System;
using System.Windows.Forms;
using System.Collections.Generic;

/// <summary>
/// mono（少なくとも6.14.1まで）の誤ったjp106レイアウトの
/// マッピングを修正するヘルパ
/// </summary>
namespace Shapoco.Platforms.Linux {
  public class WrongJP106LayoutRemapper
  {
    private static readonly Dictionary<Keys, Keys> _mapping = new Dictionary<Keys, Keys>
    {
      { Keys.Q, Keys.A },
      { Keys.W, Keys.Z },

      { Keys.A, Keys.Q },
      { Keys.Z, Keys.W },
    };

    /// <summary>
    /// 受け取った KeyEventArgs をもとに、もしマッピング表に元キーが定義されていれば
    /// KeyCode を置き換えた新しい KeyEventArgs を返します。
    /// </summary>
    public static KeyEventArgs Remap(KeyEventArgs e)
    {
      if ( !Platforms.Platform.IsX11WrongJP106() )
        return e;

      var original = e.KeyCode;

      if (_mapping.TryGetValue(original, out var mapped))
      {
        var newKeyData = mapped | e.Modifiers;
        return new KeyEventArgs(newKeyData);
      }
      return e;
    }

  }

}
