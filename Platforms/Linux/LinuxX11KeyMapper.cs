using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Shapoco.Platforms.Linux
{
  public static class LinuxX11KeyMapper
  {
    // --- X11 P/Invoke (同じ DLL は  hotkey service と共有) ---
    [DllImport("libX11.so.6")] static extern IntPtr XStringToKeysym(string s);
    [DllImport("libX11.so.6")] static extern byte XKeysymToKeycode(IntPtr d, IntPtr ks);
    [DllImport("libX11.so.6")] static extern IntPtr XOpenDisplay(string dpy);
    [DllImport("libX11.so.6")] static extern int XCloseDisplay(IntPtr d);

    /// <summary>Keys → KeySym 文字列</summary>
    public static string ConvertKeysToKeySymString(Keys key)
    {
      if (key >= Keys.A && key <= Keys.Z) return key.ToString();
      if (key >= Keys.F1 && key <= Keys.F12) return key.ToString();
      if (key >= Keys.D0 && key <= Keys.D9) return ((int)(key - Keys.D0)).ToString();
      if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        return "KP_" + (int)(key - Keys.NumPad0);

      switch (key)
      {
      case Keys.Space: return "space";
      case Keys.Enter: return "Return";
      case Keys.Escape: return "Escape";
      case Keys.Tab: return "Tab";
      case Keys.Back: return "BackSpace";
      case Keys.Delete: return "Delete";
      case Keys.Up: return "Up";
      case Keys.Down: return "Down";
      case Keys.Left: return "Left";
      case Keys.Right: return "Right";
      case Keys.Home: return "Home";
      case Keys.End: return "End";
      case Keys.PageUp: return "Page_Up";
      case Keys.PageDown: return "Page_Down";
      case Keys.Insert: return "Insert";
      case Keys.Oemcomma: return "comma";
      case Keys.OemPeriod: return "period";
      case Keys.OemQuestion: return "slash";
      case Keys.OemSemicolon: return "semicolon";
      case Keys.OemQuotes: return "apostrophe";
      case Keys.OemOpenBrackets: return "bracketleft";
      case Keys.OemCloseBrackets: return "bracketright";
      case Keys.OemPipe: return "backslash";
      case Keys.Oemtilde: return "grave";
      case Keys.Oemplus: return "equal";
      case Keys.OemMinus: return "minus";
      case Keys.Multiply: return "KP_Multiply";
      case Keys.Add: return "KP_Add";
      case Keys.Subtract: return "KP_Subtract";
      case Keys.Decimal: return "KP_Decimal";
      case Keys.Divide: return "KP_Divide";
      default: return null;      // 対応表になければ null
      }
    }

    /// <summary>
    /// X11 hardware keycode → System.Windows.Forms.Keys へ逆引き
    /// </summary>
    public static bool TryX11KeycodeToKeys(byte keycode, out Keys keys)
    {
      keys = Keys.None;
      IntPtr dpy = XOpenDisplay(null);
      if (dpy == IntPtr.Zero) return false;

      try
      {
        foreach (Keys k in Enum.GetValues(typeof(Keys)))
        {
          var sym = ConvertKeysToKeySymString(k);
          if (sym == null) continue;
          IntPtr ks = XStringToKeysym(sym);
          if (ks == IntPtr.Zero) continue;
          if (XKeysymToKeycode(dpy, ks) == keycode)
          {
            keys = k;
            return true;
          }
        }
        return false;
      }
      finally { XCloseDisplay(dpy); }
    }
  }
}
