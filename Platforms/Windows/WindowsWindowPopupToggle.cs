using Shapoco.Platforms.Common;
using Shapoco.Calctus.UI;
using System.Windows.Forms;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Shapoco.Platforms.Windows {
  class WindowsWindowPopupToggle : IWindowPopupToggle {
    readonly MainForm _mainForm;
    readonly NotifyIcon _notifyIcon;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    public WindowsWindowPopupToggle(MainForm mainForm, NotifyIcon notifyIcon) {
      _mainForm = mainForm;
      _notifyIcon = notifyIcon;
    }

    public void Toggle(object sender, EventArgs e) {
      if (GetForegroundWindow() == _mainForm.Handle)
      {
        if (_notifyIcon.Visible)
        {
          _mainForm.Visible = false;
        }
        else
        {
          _mainForm.WindowState = FormWindowState.Minimized;
        }
      }
      else
      {
        _mainForm.ShowForeground();
      }
    }
  }
}
