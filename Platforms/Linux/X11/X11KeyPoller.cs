using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Shapoco.Platforms.Linux.X11 {
  /// <summary>
  /// X11のキー状態をポーリングし、変化を購読者に通知するシングルトン
  /// </summary>
  public sealed class X11KeyPoller : IDisposable
  {
    private const int POLLING_T = 50; // ポーリング間隔 ms

    private static readonly object _instanceLock = new object();
    private static X11KeyPoller _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static X11KeyPoller Instance
    {
      get
      {
        lock (_instanceLock)
        {
          if (_instance == null)
          {
            _instance = new X11KeyPoller();
          }
          return _instance;
        }
      }
    }

    private readonly object _sync = new object();
    private int _subscriberCount = 0;
    private CancellationTokenSource _cts;
    private Task _pollTask;
    private X11DisplayManager _dmgr;
    private IntPtr _display;
    private readonly byte[] _prevMap = new byte[32];
    private event Action<byte[]> _innerUpdated;

    private X11KeyPoller() { /* lazy init only */ }

    /// <summary>
    /// キー状態が変化すると発火 (256-bit map)
    /// アップデート関数を登録することで、キー状態の変化を受け取ることができる
    /// </summary>
    public event Action<byte[]> KeyMapUpdated
    {
      add
      {
        lock (_sync)
        {
          _innerUpdated += value;
          if (++_subscriberCount == 1) StartPolling();
#if DEBUG
          Console.WriteLine($"[DBG X11KeyPoller] Subscriber ++count =>: {_subscriberCount}");
#endif
        }
      }
      remove
      {
        lock (_sync)
        {
          if (_subscriberCount > 0) {
            _innerUpdated -= value;
            _subscriberCount--;
          }
          if (_subscriberCount == 0) StopPolling();
#if DEBUG
          Console.WriteLine($"[DBG X11KeyPoller] Subscriber --count =>: {_subscriberCount}");
#endif
        }
      }
    }

    //void hoge(byte[] keymap) { Console.WriteLine("innerUpdated"); }

    /// <summary>ポーリングを開始する</summary>
    private void StartPolling()
    {
#if DEBUG
      Console.WriteLine("[DBG X11KeyPoller] Start Polling.");
      //_innerUpdated += hoge;
#endif
      _dmgr = X11DisplayManager.Instance;
      _display = _dmgr.Display;
      _cts = new CancellationTokenSource();
      _pollTask = Task.Run(() => PollLoop(_cts.Token), _cts.Token);
    }

    /// <summary>ポーリングを停止する</summary>
    private void StopPolling()
    {
#if DEBUG
      Console.WriteLine("[DBG X11KeyPoller] Stop Polling.");
#endif
      try
      {
        _cts?.Cancel();
        _pollTask?.Wait();
      }
      catch { /* ignore */ }
      finally
      {
        _cts?.Dispose();
        _cts = null;
        _pollTask = null;

        _dmgr?.Release();
        _dmgr = null;
        _display = IntPtr.Zero;

        Array.Clear(_prevMap, 0, _prevMap.Length);
      }
    }

    private async Task PollLoop(CancellationToken token)
    {
      var map = new byte[32];
      while (!token.IsCancellationRequested)
      {
        try
        {
          XQueryKeymap(_display, map);
          if (!MapsAreEqual(map, _prevMap))
          {
            _innerUpdated?.Invoke(map);
            Buffer.BlockCopy(map, 0, _prevMap, 0, map.Length);
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[X11KeyPoller] Error polling keymap: {ex}");
        }
        try { await Task.Delay(POLLING_T, token).ConfigureAwait(false); }
        catch (TaskCanceledException) { break; }
      }
    }

    private bool MapsAreEqual(byte[] a, byte[] b)
    {
      if (a == null || b == null || a.Length != b.Length) return false;
      for (int i = 0; i < a.Length; i++)
      {
        if (a[i] != b[i]) return false;
      }
      return true;
    }

    /// <summary>
    /// ポーラーを破棄
    /// </summary>
    public void Dispose()
    {
      lock (_sync)
      {
        if (_subscriberCount > 0)
        {
          _subscriberCount = 0;
          StopPolling();
#if DEBUG
          Console.WriteLine("[DBG X11KeyPoller] Disposed.");
#endif
        }
      }
    }

    [DllImport("libX11.so.6")]
    private static extern int XQueryKeymap(IntPtr display, [Out] byte[] keys);
  }
}
