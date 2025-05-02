using System;
using System.Drawing;
using System.Windows.Forms;

namespace Shapoco.Platforms.Common
{
    public static class DpiHelper
    {
        /// <summary>
        /// Control.DeviceDpi が使えるならそれで、使えない環境では CreateGraphics().DpiX で DPI を返す。
        /// </summary>
        public static int GetDeviceDpi(Control ctrl)
        {
          // Mono の WinForms では DeviceDpi が未実装なのでこちらを使う
          using (var g = ctrl.CreateGraphics())
          {
            return (int)g.DpiX;
          }
        }
    }
}
