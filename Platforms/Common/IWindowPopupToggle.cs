using System;

namespace Shapoco.Platforms.Common {
  public interface IWindowPopupToggle {
    /// <summary>メインウィンドウの出没トグル</summary>
    void Toggle(object sender, EventArgs e);
  }
}
