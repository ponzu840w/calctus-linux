using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shapoco.Platforms {
  public static class Platform {
    // 起動時に１度だけ評価される
    static readonly bool _isMono;
    static readonly bool _isUnix;
    static readonly bool _isWindows;
    static readonly bool _isWine;
    static readonly bool _isFlatpak;
    static readonly PlatformID _platform_id;
    static readonly string _platform_description;
    static readonly string _flatpakAppId;
    static readonly string _sessionType;
    static readonly string _displayEnv;
    static readonly string _waylandEnv;

    // static コンストラクタ
    static Platform() {
      // 基本情報
      _platform_id = Environment.OSVersion.Platform;
      _isMono = Type.GetType("Mono.Runtime") != null;
      _isUnix = _isMono && _platform_id == PlatformID.Unix;
      _isWindows = _platform_id == PlatformID.Win32NT;

      // Wine
      _isWine = _checkIfWine();

      // Flatpak
      _isFlatpak = _isUnix && File.Exists("/.flatpak-info");
      if (_isFlatpak) _flatpakAppId = Environment.GetEnvironmentVariable("FLATPAK_ID");
      else _flatpakAppId = null;

      // Unix GUI
      if (_isUnix) {
        var sess = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
        _sessionType = (sess != null) ? sess.ToLowerInvariant() : string.Empty;
        var disp = Environment.GetEnvironmentVariable("DISPLAY");
        _displayEnv = (disp != null) ? disp : string.Empty;
        var wayl = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        _waylandEnv = (wayl != null) ? wayl : string.Empty;
      } else {
        _sessionType = string.Empty;
        _displayEnv  = string.Empty;
        _waylandEnv  = string.Empty;
      }

      // プラットフォームを端的に表す文字列を作成
      _platform_description = _platform_id.ToString();
      //if (_isMono) _platform_description    += "-Mono"; // UnixならMonoは自明だよな…
      if (_isWine) _platform_description    += "-Wine";
      if (_isFlatpak) _platform_description += "-FP";
      Console.WriteLine($"Platform: {_platform_description}");
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    private static bool _checkIfWine() {
      if (_isWindows == false) return false;
      IntPtr ntdll = GetModuleHandle("ntdll.dll");
      if (ntdll == IntPtr.Zero) {
        ntdll = LoadLibrary("ntdll.dll");
        if (ntdll == IntPtr.Zero) return false;
      }
      IntPtr proc = GetProcAddress(ntdll, "wine_get_version"); // wine 固有の関数
      return proc != IntPtr.Zero;
    }

    public static bool IsMono()        => _isMono;
    public static bool IsWindows()     => _isWindows;
    public static bool IsLinuxMono()   => _isMono && _isUnix;
    public static bool IsFlatpak()     => _isFlatpak;
    public static bool IsWine()        => _isWine;
    public static bool IsUnix()        => _isUnix;
    public static bool IsPureX11()     => _displayEnv.Length > 0 && _waylandEnv.Length == 0;
    public static bool IsPureWayland() => _waylandEnv.Length > 0 && _displayEnv.Length == 0;
    public static bool IsXWayland()    => _sessionType == "wayland" && _displayEnv.Length > 0;
    public static bool IsX11()         => IsPureX11() || IsXWayland();
    public static bool IsWayland()     => IsPureWayland() || IsXWayland();
    public static string GetFlatpakAppId()  => _flatpakAppId ?? string.Empty;
    public static string GetPlatformDescription() => _platform_description;
  }
}
