using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Shapoco.Calctus;
using Shapoco.Calctus.UI;

namespace Shapoco.Platforms.Common {
  public interface IWindowPopupToggle {
    /// <summary>ウィンドウの出没</summary>
    void Toggle(object sender, EventArgs e);
  }
}
