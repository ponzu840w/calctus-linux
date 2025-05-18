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
    private Common.ModifierKey _registeredModifiers = Common.ModifierKey.None;
    private byte _registeredKeycode = 0; // X11 keycode
    private Dictionary<Common.ModifierKey, List<byte>> _modifierKeycodes; // 修飾キーEnum->X11キーのマップ
    private bool hotkeyCurrentlyPressed = false;

    private readonly X11DisplayManager _dmgr;
    private readonly X11KeyPoller _poller;

    public event EventHandler HotKeyPressed;

    public LinuxX11HotKeyService()
    {
      _dmgr = X11DisplayManager.Instance;
      _modifierKeycodes = InitializeModifierKeycodes();
      //_poller = new X11KeyPoller(_dmgr.Display);
      _poller = X11KeyPoller.Instance;
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
      // 既存の登録があれば解除
      if (_registeredKeycode != 0)
      {
        Console.WriteLine($"Warning: Hotkey already registered. Unregistering previous hotkey.");
        Unregister();
      }

      _registeredModifiers = modifiers;
      _registeredKeycode = X11KeyMapper.ConvertKeysToX11Keycode(_dmgr.Display, key);

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

      _poller.KeyMapUpdated += OnKeyMapUpdated;
      Console.WriteLine($"[X11 HotKey] Registered hotkey: Modifiers={modifiers}, Key={key} (X11 Keycode={_registeredKeycode})");
      return true;
    }

    public void Unregister()
    {
      _poller.KeyMapUpdated -= OnKeyMapUpdated;
      _registeredKeycode = 0;
      _registeredModifiers = Common.ModifierKey.None;
#if Debug
      Console.WriteLine("[DBG X11 HotKey] Unregistered Linux hotkey.");
#endif
    }

    private void OnKeyMapUpdated(byte[] keymap)
    {
      bool allModifiersPressed = true;
      if (_registeredModifiers != Common.ModifierKey.None)
      {
        foreach (Common.ModifierKey modFlag in Enum.GetValues(typeof(Common.ModifierKey)))
        {
          if (modFlag == Common.ModifierKey.None) continue;
          if ((_registeredModifiers & modFlag) == modFlag)
          {
            var codes = _modifierKeycodes[modFlag];
            if (!codes.Any(code => IsX11KeyPressed(keymap, code)))
            {
              allModifiersPressed = false;
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
    }

    private bool IsX11KeyPressed(byte[] keymap, byte keycode)
    {
      int byteIndex = keycode / 8;
      int bitIndex = keycode % 8;
      if (byteIndex < 0 || byteIndex >= keymap.Length) return false; // 範囲外チェック
      return (keymap[byteIndex] & (1 << bitIndex)) != 0;
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
