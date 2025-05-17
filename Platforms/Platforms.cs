using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shapoco.Platforms {
  public static class Platform {
      // 起動時に１度だけ評価される
      static readonly bool _isMono;
      static readonly bool _isUnix;
      static readonly bool _isWine;
      static readonly bool _isFlatpak;
      static readonly string _flatpakAppId;
      static readonly string _platform_description;

      // static コンストラクタ
      static Platform() {
          _isMono = Type.GetType("Mono.Runtime") != null;
          _isUnix = Environment.OSVersion.Platform == PlatformID.Unix;
          _isFlatpak = _isUnix && File.Exists("/.flatpak-info");
          _isWine = CheckIfWine();
          _flatpakAppId = Environment.GetEnvironmentVariable("FLATPAK_ID");
          _platform_description = Environment.OSVersion.Platform.ToString();
          /* UnixならMonoは自明だよな…
          if (_isMono) {
              _platform_description += "-Mono";
          }
          */
          if (_isWine) {
              _platform_description += "-Wine";
          }
          if (_isFlatpak) {
              _platform_description += "-FP";
          }
          Console.WriteLine($"Platform: {_platform_description}");
      }

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

      [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadLibrary(string lpFileName);

      [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

      static bool CheckIfWine() {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
          return false;
        }
        IntPtr ntdll = GetModuleHandle("ntdll.dll");
        if (ntdll == IntPtr.Zero) {
          ntdll = LoadLibrary("ntdll.dll");
          if (ntdll == IntPtr.Zero) return false;
        }
        // wine 固有の関数
        IntPtr proc = GetProcAddress(ntdll, "wine_get_version");
        return proc != IntPtr.Zero;
      }

      // AggressiveInlining でインライン化
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsMono() => _isMono;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsWindows() =>
          !_isMono && Environment.OSVersion.Platform == PlatformID.Win32NT;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsLinuxMono() =>
          _isMono && _isUnix;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsFlatpak() =>
          _isFlatpak;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static string GetFlatpakAppId() =>
          _flatpakAppId ?? string.Empty;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsWine() =>
          _isWine;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsUnix() =>
          _isUnix;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static string GetPlatformDescription() =>
          _platform_description;
  }
}
