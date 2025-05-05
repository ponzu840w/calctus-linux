using System;
using System.Runtime.CompilerServices;

namespace Shapoco.Platforms {
  public static class Platform
  {
      // 起動時に１度だけ評価される
      static readonly bool _isMono;
      static readonly bool _isUnix;

      // static コンストラクタ
      static Platform()
      {
          _isMono = Type.GetType("Mono.Runtime") != null;
          _isUnix = Environment.OSVersion.Platform == PlatformID.Unix;
      }

      // プロパティには AggressiveInlining を付けておくと
      // JIT がインライン展開して呼び出しオーバーヘッドを極小化してくれます
      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsMono() => _isMono;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsWindows() =>
          !_isMono && Environment.OSVersion.Platform == PlatformID.Win32NT;

      [MethodImpl(MethodImplOptions.AggressiveInlining)]
      public static bool IsLinuxMono() =>
          _isMono && _isUnix;
  }
}
