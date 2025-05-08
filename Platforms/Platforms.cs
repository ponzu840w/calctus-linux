using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Shapoco.Platforms {
  public static class Platform {
      // 起動時に１度だけ評価される
      static readonly bool _isMono;
      static readonly bool _isUnix;
      static readonly bool _isFlatpak;
      static readonly string _flatpakAppId;

      // static コンストラクタ
      static Platform() {
          _isMono = Type.GetType("Mono.Runtime") != null;
          _isUnix = Environment.OSVersion.Platform == PlatformID.Unix;
          _isFlatpak = _isUnix && File.Exists("/.flatpak-info");
          _flatpakAppId = Environment.GetEnvironmentVariable("FLATPAK_ID");
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
  }
}
