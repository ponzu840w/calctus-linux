using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shapoco.Platforms.Linux.X11 {
  /// <summary>
  /// Singleton: polls X11 keymap and fires updates to subscribers.
  /// </summary>
  public sealed class X11KeyPoller : IDisposable {
    private static readonly Lazy<X11KeyPoller> _lazy = new Lazy<X11KeyPoller>(() => new X11KeyPoller());
    public static X11KeyPoller Instance => _lazy.Value;

    private readonly IntPtr _display;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly Task _task;
    private readonly byte[] _prevMap = new byte[32];

    /// <summary>Fires on each poll with the latest 256-bit key map.</summary>
    public event Action<byte[]> KeyMapUpdated;

    private X11KeyPoller() {
      _display = X11DisplayManager.Instance.Display;
      _task = Task.Run(PollLoop, _cts.Token);
    }

    private async Task PollLoop() {
      var map = new byte[32];
      while (!_cts.Token.IsCancellationRequested) {
        XQueryKeymap(_display, map);
        KeyMapUpdated?.Invoke(map);
        Buffer.BlockCopy(map, 0, _prevMap, 0, 32);
        await Task.Delay(50, _cts.Token).ConfigureAwait(false);
      }
    }

    public void Dispose() {
      _cts.Cancel();
      try { _task.Wait(); } catch { }
      X11DisplayManager.Instance.Release();
    }

    [DllImport("libX11.so.6")]
    private static extern int XQueryKeymap(IntPtr display, [Out] byte[] keys);
  }
}
