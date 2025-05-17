using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Shapoco.Platforms.Linux
{

  // https://github.com/culajunge/LinuxGlobalHotkeys
  public class LinuxX11HotKeyService : Common.IHotKeyService
  {
    // --- X11 P/Invoke
    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(string display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    // XStringToKeysym と XKeysymToKeycode は直接使わず、Keys enum から直接 X11 KeyCode へマッピングする
    [DllImport("libX11.so.6")]
    private static extern IntPtr XStringToKeysym(string keysym); // マッピングテーブル生成時の補助として使用可能性

    [DllImport("libX11.so.6")]
    private static extern byte XKeysymToKeycode(IntPtr display, IntPtr keysym); // マッピングテーブル生成時の補助として使用可能性

    [DllImport("libX11.so.6")]
    private static extern int XQueryKeymap(IntPtr display, [Out] byte[] keys);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDisplayKeycodes(IntPtr display, out int min_keycode_return, out int max_keycode_return);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XGetKeyboardMapping(IntPtr display, byte first_keycode, int keycode_count, out int keysyms_per_keycode_return);

    [DllImport("libX11.so.6")]
    private static extern void XFree(IntPtr data);
    // --- X11 定数 ---
    // X11 Modifier masks
    // private const uint ShiftMask = (1 << 0);
    // private const uint LockMask = (1 << 1);   // Caps Lock
    // private const uint ControlMask = (1 << 2);
    // private const uint Mod1Mask = (1 << 3);    // Alt
    // private const uint Mod2Mask = (1 << 4);    // Num Lock
    // private const uint Mod3Mask = (1 << 5);
    // private const uint Mod4Mask = (1 << 6);    // Super/Win
    // private const uint Mod5Mask = (1 << 7);

    private IntPtr _display;
    private CancellationTokenSource _cts;
    private Task _listenerTask;

    private Common.ModifierKey _registeredModifiers = Common.ModifierKey.None;
    private byte _registeredKeycode = 0; // X11 keycode
    private Dictionary<Common.ModifierKey, byte> _modifierKeycodes; // 修飾キーEnumとX11キーコードのマッピング

    public event EventHandler HotKeyPressed;

    public LinuxX11HotKeyService()
    {
      _display = XOpenDisplay(null);
      if (_display == IntPtr.Zero)
      {
#if Debug
        Console.WriteLine("[DBG X11HotKey] Failed to open X11 display. Make sure X11/XWayland is running.");
#endif
      }

      // 修飾キーとX11キーコードのマッピングを初期化
      _modifierKeycodes = InitializeModifierKeycodes();
    }

    private Dictionary<Common.ModifierKey, byte> InitializeModifierKeycodes()
    {
      if (_display == IntPtr.Zero) return null;
      var mapping = new Dictionary<Common.ModifierKey, byte>();
      // これらのキーコードは環境によって異なる可能性があるため、本来は動的に取得するのが望ましい
      // XStringToKeysym と XKeysymToKeycode を使って初期化時に解決できる
      // 例: mapping[Common.ModifierKey.Shift] = XKeysymToKeycode(_display, XStringToKeysym("Shift_L"));
      //     GlobalHotkeyManager.cs の modifierKeycodes も固定値だったため、それに倣うか、より堅牢な方法を検討
      //     ここでは簡略化のため、一般的なキーコードを仮定 (GlobalHotkeyManager.csの値を参考)
      //     より堅牢にするには、Xkb XGetModifierMapping を使う必要があるかもしれません。

      // GlobalHotkeyManager.cs の modifierKeycodes にあった値 (Left Shift, Left Ctrl, Left Alt, Left Super)
      // これらのキーコードは 'xmodmap -pk' などで確認できるシステムの実際のキーコードに合わせるのが理想
      // Keysym 文字列から Keycode を引く方が移植性は高い
      mapping[Common.ModifierKey.Shift] = GetKeycodeForKeysym("Shift_L"); // 左Shiftキーのキーコード
      mapping[Common.ModifierKey.Ctrl] = GetKeycodeForKeysym("Control_L"); // 左Ctrlキーのキーコード
      mapping[Common.ModifierKey.Alt] = GetKeycodeForKeysym("Alt_L"); // 左Altキーのキーコード
      mapping[Common.ModifierKey.Win] = GetKeycodeForKeysym("Super_L"); // 左Super/Winキーのキーコード

      // 右側の修飾キーも考慮に入れる場合は追加する
      // mapping[Common.ModifierKey.Shift_R] = GetKeycodeForKeysym("Shift_R");
      // mapping[Common.ModifierKey.Ctrl_R] = GetKeycodeForKeysym("Control_R");
      // ...など

      // 存在しないキーコード（0）が返ってきた場合のフィルタリング
      var validMapping = new Dictionary<Common.ModifierKey, byte>();
      foreach (var kvp in mapping)
      {
        if (kvp.Value != 0)
        {
          validMapping.Add(kvp.Key, kvp.Value);
        }
        else
        {
          Console.WriteLine($"Warning: Could not find keycode for modifier keysym for {kvp.Key}. This modifier might not work.");
        }
      }
      return validMapping;
    }

    private byte GetKeycodeForKeysym(string keysymName)
    {
      if (_display == IntPtr.Zero) return 0;
      IntPtr keysym = XStringToKeysym(keysymName);
      if (keysym == IntPtr.Zero) return 0;
      return XKeysymToKeycode(_display, keysym);
    }


    public bool Register(Common.ModifierKey modifiers, Keys key)
    {
      if (_display == IntPtr.Zero) return false;
      if (_listenerTask != null && !_listenerTask.IsCompleted)
      {
        // 既にリスナーが動作中 = 何か登録済み
        Unregister(); // 一旦解除
      }

      _registeredModifiers = modifiers;
      _registeredKeycode = ConvertKeysToX11Keycode(key);

      if (_registeredKeycode == 0)
      {
        Console.WriteLine($"Failed to get X11 keycode for key: {key}");
        return false;
      }

      // 必要な修飾キーがマッピングで見つかるか確認
      if (modifiers != Common.ModifierKey.None)
      {
        foreach (Common.ModifierKey modFlag in Enum.GetValues(typeof(Common.ModifierKey)))
        {
          if (modFlag == Common.ModifierKey.None) continue;
          if ((modifiers & modFlag) == modFlag && !_modifierKeycodes.ContainsKey(modFlag))
          {
            Console.WriteLine($"Warning: Keycode for modifier {modFlag} is not available or mapped. Hotkey might not work as expected.");
            // 登録を失敗させるか、警告に留めるか
            // return false;
          }
        }
      }

      _cts = new CancellationTokenSource();
      _listenerTask = StartKeyStateListener(_cts.Token);

      Console.WriteLine($"[X11 HotKey] Registered hotkey: Modifiers={modifiers}, Key={key} (X11 Keycode={_registeredKeycode})");
      return true;
    }

    public void Unregister()
    {
      if (_cts != null)
      {
        _cts.Cancel();
        try
        {
          _listenerTask?.Wait(500); // 少し待つ
        }
        catch (AggregateException ex)
        {
          ex.Handle(e => e is TaskCanceledException);
        }
        _cts.Dispose();
        _cts = null;
      }
      _listenerTask = null;
      _registeredKeycode = 0;
      _registeredModifiers = Common.ModifierKey.None;
#if Debug
      Console.WriteLine("[DBG X11 HotKey] Unregistered Linux hotkey.");
#endif
    }

    private Task StartKeyStateListener(CancellationToken cancellationToken)
    {
      return Task.Run(() => {
#if Debug
          Console.WriteLine("[DBG X11 HotKey] Linux global hotkey listener started.");
#endif
          byte[] keymap = new byte[32]; // 256 bits for key states
          bool hotkeyCurrentlyPressed = false;

          while (!cancellationToken.IsCancellationRequested)
          {
          if (_registeredKeycode == 0)
          { // 何も登録されていなければ何もしない
          Thread.Sleep(100);
          continue;
          }

          XQueryKeymap(_display, keymap);

          bool allModifiersPressed = true;
          if (_registeredModifiers != Common.ModifierKey.None)
          {
          foreach (Common.ModifierKey modFlag in Enum.GetValues(typeof(Common.ModifierKey)))
          {
            if (modFlag == Common.ModifierKey.None) continue;

            if ((_registeredModifiers & modFlag) == modFlag)
            { // この修飾キーが必要
              if (_modifierKeycodes.TryGetValue(modFlag, out byte modKeycode))
              {
                if (!IsX11KeyPressed(keymap, modKeycode))
                {
                  allModifiersPressed = false;
                  break;
                }
              }
              else
              {
                // マッピングにない修飾キーが指定された場合（警告済み）
                allModifiersPressed = false; // 安全のため、押されていないとみなす
                break;
              }
            }
          }
          }
          else
          {
            // 修飾キーなしの場合、他の修飾キーが押されていないことを確認するかどうか？
            // (例: Alt+A を登録していて、Ctrl+Alt+A が押された場合も発火させるか)
            // ここでは、指定された修飾キーのみをチェックする。
            // もし「指定外の修飾キーが押されていたら発火しない」という仕様なら、
            // _modifierKeycodes に含まれるキーのうち、_registeredModifiers にないものが
            // 押されていないことを確認するロジックを追加する。
          }

          bool mainKeyPressed = IsX11KeyPressed(keymap, _registeredKeycode);

          if (allModifiersPressed && mainKeyPressed)
          {
            if (!hotkeyCurrentlyPressed)
            {
              hotkeyCurrentlyPressed = true;
#if Debug
              Console.WriteLine("[DBG X11 HotKey] Linux hotkey triggered.");
#endif
              // イベントをUIスレッドで実行する必要があれば Task.Factory.StartNew などで調整
              try {
                HotKeyPressed?.Invoke(this, EventArgs.Empty);
              }
              catch (Exception ex) {
                Console.WriteLine($"HotKeyPressed handler error: {ex}");
                // ループは継続
              }
            }
          }
          else
          {
            if (hotkeyCurrentlyPressed)
            {
              hotkeyCurrentlyPressed = false;
#if Debug
              Console.WriteLine("[DBG X11 HotKey] Linux hotkey released.");
#endif
            }
          }
          Thread.Sleep(50); // ポーリング間隔 (GlobalHotkeyManager.cs と同じ)
          }
#if Debug
          Console.WriteLine("[DBG X11 HotKey] Linux global hotkey listener stopped.");
#endif
      }, cancellationToken);
    }

    private bool IsX11KeyPressed(byte[] keymap, byte keycode)
    {
      int byteIndex = keycode / 8;
      int bitIndex = keycode % 8;
      if (byteIndex < 0 || byteIndex >= keymap.Length) return false; // 範囲外チェック
      return (keymap[byteIndex] & (1 << bitIndex)) != 0;
    }

    private byte ConvertKeysToX11Keycode(Keys key)
    {
      // System.Windows.Forms.Keys から X11 Keycode への直接的なマッピング
      // これは環境依存性が高いため、理想は XStringToKeysym -> XKeysymToKeycode を使うこと。
      // 初期化時にマッピングテーブルを生成しておくのが良い。
      // ここでは、GlobalHotkeyManager.cs の手法を踏襲せず、より直接的な方法を試みる。
      // (ただし、XKeysymToKeycode を使うアプローチがより堅牢)

      // 例: よく使うキーのみマッピング。本番ではもっと網羅的なテーブルが必要
      string keysymName = ConvertKeysToKeySymStringInternal(key);
      if (string.IsNullOrEmpty(keysymName)) return 0;

      return GetKeycodeForKeysym(keysymName);
    }

    // 前回の回答にあった ConvertKeysToKeySymString を内部ヘルパーとして利用
    private string ConvertKeysToKeySymStringInternal(Keys key)
    {
      if (key >= Keys.A && key <= Keys.Z) return key.ToString();
      if (key >= Keys.D0 && key <= Keys.D9) return (key - Keys.D0).ToString();
      if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return "KP_" + (key - Keys.NumPad0).ToString();

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
      case Keys.F1: return "F1";
      case Keys.F2: return "F2"; /* ... F12まで ... */
      case Keys.F12: return "F12";
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
      default: return null;
      }
    }

    public void Dispose()
    {
      Unregister();
      if (_display != IntPtr.Zero)
      {
        XCloseDisplay(_display);
        _display = IntPtr.Zero;
      }
#if Debug
      Console.WriteLine("[DBG X11 HotKey] LinuxHotKeyService disposed.");
#endif
    }
  }
}
