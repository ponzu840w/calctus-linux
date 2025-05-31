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
      { Keys.Q, Keys.A }, { Keys.A, Keys.Q },
      { Keys.W, Keys.Z }, { Keys.Z, Keys.W },
      { Keys.Oem7,  Keys.D1 },
      { Keys.D1,    Keys.D2 },
      { Keys.D2,    Keys.D3 },
      { Keys.D3,    Keys.D4 },
      { Keys.D4,    Keys.D5 },
      { Keys.D5,    Keys.D6 },
      { Keys.D6,    Keys.D7 },
      { Keys.D7,    Keys.D8 },
      { Keys.D8,    Keys.D9 },
      { Keys.D9,    Keys.D0 },
      { Keys.D0,    Keys.OemMinus },
      { Keys.Oem4,              Keys.Oem7 },
      //{ Keys.Oem102,            Keys.Oem5 },  // 円記号はバックスラッシュと同一視される
      { Keys.OemCloseBrackets,  Keys.Oemtilde },
      { Keys.Oem1,              Keys.OemOpenBrackets },
      { Keys.M,                 Keys.Oemplus },
      { Keys.Oemtilde,          Keys.Oem1 },
      { Keys.OemPipe,           Keys.OemCloseBrackets },
      { Keys.Oemcomma,          Keys.M },
      { Keys.OemPeriod,         Keys.Oemcomma },
      { Keys.OemQuestion,       Keys.OemPeriod },
      { Keys.Oem8,              Keys.OemQuestion },
      { Keys.Oem102,            Keys.OemBackslash },
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
