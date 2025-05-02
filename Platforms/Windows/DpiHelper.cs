using System;
using System.Drawing;
using System.Windows.Forms;

namespace Shapoco.Calctus.Platforms.Common
{
    public static class DpiHelper
    {
        /// <summary>
        /// Control.DeviceDpi が使えるならそれで、使えない環境では CreateGraphics().DpiX で DPI を返す。
        /// </summary>
        public static int GetDeviceDpi(Control ctrl)
        {
          // .NET Framework 4.7+ on Windows
          return ctrl.DeviceDpi;
        }
    }
}
