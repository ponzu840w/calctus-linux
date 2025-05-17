using System;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Shapoco.Platforms.Linux.X11;

namespace Shapoco.Platforms.Linux
{
  // https://github.com/culajunge/LinuxGlobalHotkeys
  public class LinuxX11HotKeyService : Common.IHotKeyService, IDisposable
  {
    private const int POLLING_T = 50; // ポーリング間隔 ms

    // --- X11 P/Invoke
    [DllImport("libX11.so.6")]
    private static extern int XQueryKeymap(IntPtr display, [Out] byte[] keys);

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

    private CancellationTokenSource _cts;
    private Task _listenerTask;

    private Common.ModifierKey _registeredModifiers = Common.ModifierKey.None;
    private byte _registeredKeycode = 0; // X11 keycode
    private Dictionary<Common.ModifierKey, List<byte>> _modifierKeycodes; // 修飾キーEnum->X11キーのマップ

    public event EventHandler HotKeyPressed;

    private readonly X11DisplayManager _dmgr;

    public LinuxX11HotKeyService()
    {
      _dmgr = X11DisplayManager.Instance;
      _modifierKeycodes = InitializeModifierKeycodes();
    }

    // 装飾キーの->X11キーコードマッピングを初期化
    private Dictionary<Common.ModifierKey, List<byte>> InitializeModifierKeycodes()
    {
      if (_dmgr.Display == IntPtr.Zero) return null;
      var symNames = new Dictionary<Common.ModifierKey, string[]>
      {
        [Common.ModifierKey.Shift] = new[] { "Shift_L",   "Shift_R"   },
        [Common.ModifierKey.Ctrl]  = new[] { "Control_L", "Control_R" },
        [Common.ModifierKey.Alt]   = new[] { "Alt_L",     "Alt_R"     },
        [Common.ModifierKey.Win]   = new[] { "Super_L",   "Super_R"   }
      };
      return symNames.ToDictionary(
          kvp => kvp.Key,
          kvp => GetModifierKeyCodes(kvp.Value)
          );
    }

    // 文字列 KeySym の配列を受け取り、有効な KeyCode のリストを返す
    private List<byte> GetModifierKeyCodes(params string[] keySyms) =>
      keySyms.Select(item => X11KeyMapper.GetKeycodeForKeysym(_dmgr.Display, item))
      .Where(code => code != 0)
      .ToList();

    public bool Register(Common.ModifierKey modifiers, Keys key)
    {
      if (_dmgr.Display == IntPtr.Zero) return false;
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

    // キー状態リスナー
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

            XQueryKeymap(_dmgr.Display, keymap);

            bool allModifiersPressed = true;
            if (_registeredModifiers != Common.ModifierKey.None)
            {
              foreach (Common.ModifierKey modFlag in Enum.GetValues(typeof(Common.ModifierKey)))
              {
                if (modFlag == Common.ModifierKey.None) continue;

                if ((_registeredModifiers & modFlag) == modFlag)
                { // この修飾キーが必要
                  if (_modifierKeycodes.TryGetValue(modFlag, out var keycodes))
                  {
                    bool pressed = keycodes.Any(code => IsX11KeyPressed(keymap, code));
                    if (!pressed)
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
            Thread.Sleep(POLLING_T);
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
      // System.Windows.Forms.Keys から X11 Keycode へのマッピング
      string keysymName = X11KeyMapper.ConvertKeysToX11KeySymString(key);
      if (string.IsNullOrEmpty(keysymName)) return 0;

      return X11KeyMapper.GetKeycodeForKeysym(_dmgr.Display, keysymName);
    }

    public void Dispose()
    {
      Unregister();
      _dmgr.Release();
#if Debug
      Console.WriteLine("[DBG X11 HotKey] LinuxHotKeyService disposed.");
#endif
    }
  }
}
