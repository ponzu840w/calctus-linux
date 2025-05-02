using System;
using System.Drawing;
using System.Windows.Forms;

namespace Shapoco.Calctus.Platforms.Helpers
{
    public static class DpiHelper
    {
        /// <summary>
        /// Control.DeviceDpi が使えない環境では CreateGraphics().DpiX を使うフォールバック付きで DPI を返す。
        /// </summary>
        public static int GetDeviceDpi(Control ctrl)
        {
          /*
            try
            {
                // .NET Framework 4.7+ on Windows
                return ctrl.DeviceDpi;
            }
            catch (MissingMethodException)
            {
              */
                // Mono の WinForms では DeviceDpi が未実装なのでこちらを使う
                using (var g = ctrl.CreateGraphics())
                {
                    return (int)g.DpiX;
                }
            //}
        }
    }
}
