using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Shapoco.Platforms;
using Shapoco.Platforms.Linux;
using System.Threading;

namespace Shapoco.Calctus.UI {
  public class KeyCodeBox : Panel, IDisposable {
    const int MONO_MAX_WIDTH = 50; // Mono の場合、横幅を制限する

    public event EventHandler KeyCodeChanged;

    private CheckBox _winBox = new CheckBox();
    private CheckBox _altBox = new CheckBox();
    private CheckBox _ctrlBox = new CheckBox();
    private CheckBox _shiftBox = new CheckBox();
    private TextBox _keyCodeBox = new TextBox();

    private bool _win = false;
    private bool _alt = false;
    private bool _ctrl = false;
    private bool _shift = false;
    private Keys _keyCode = Keys.None;

    readonly X11KeyListener _x11KeyListener;

    public KeyCodeBox() {
      if (this.DesignMode) {
        var label = new Label();
        label.Text = nameof(KeyCodeBox);
        this.Controls.Add(label);
        return;
      }

      _winBox.TabIndex = 0;
      _altBox.TabIndex = 1;
      _ctrlBox.TabIndex = 2;
      _shiftBox.TabIndex = 3;
      _keyCodeBox.TabIndex = 4;
      _winBox.Dock = DockStyle.Left;
      _altBox.Dock = DockStyle.Left;
      _ctrlBox.Dock = DockStyle.Left;
      _shiftBox.Dock = DockStyle.Left;
      _keyCodeBox.Dock = DockStyle.Fill;
      _winBox.Text = "Win";
      _altBox.Text = "Alt";
      _ctrlBox.Text = "Ctrl";
      _shiftBox.Text = "Shift";
      var boxes = new[] { _winBox, _altBox, _ctrlBox, _shiftBox };
      foreach (var box in boxes) {
        var sz = box.PreferredSize;
        // Mono の場合だけ、横幅を上限でクリップ
        if (Platform.IsMono()) {
          sz = new Size(Math.Min(sz.Width, MONO_MAX_WIDTH), sz.Height);
        }
        box.AutoSize = false;
        box.Size = sz;
      }
      _keyCodeBox.AutoSize = false;
      _keyCodeBox.Size = _keyCodeBox.PreferredSize;
      this.Controls.Add(_keyCodeBox);
      this.Controls.Add(_shiftBox);
      this.Controls.Add(_ctrlBox);
      this.Controls.Add(_altBox);
      this.Controls.Add(_winBox);

      _winBox.CheckedChanged += (s, e) => { this.Win = ((CheckBox)s).Checked; };
      _altBox.CheckedChanged += (s, e) => { this.Alt = ((CheckBox)s).Checked; };
      _ctrlBox.CheckedChanged += (s, e) => { this.Ctrl = ((CheckBox)s).Checked; };
      _shiftBox.CheckedChanged += (s, e) => { this.Shift = ((CheckBox)s).Checked; };
      _keyCodeBox.KeyDown += _keyBox_KeyDown;
      _keyCodeBox.KeyUp += _keyBox_KeyUp;
      _keyCodeBox.KeyPress += _keyBox_KeyPress;

      if (Platform.IsMono() && Platform.IsX11())
      {
        _x11KeyListener = new X11KeyListener(this);
        _keyCodeBox.Enter += (_, __) => _x11KeyListener.SetActive(true);
        _keyCodeBox.Leave += (_, __) => _x11KeyListener.SetActive(false);
      }
    }

    private void _keyBox_KeyDown(object sender, KeyEventArgs e) {
      e.Handled = true;
      if (Platform.IsMono() && Platform.IsX11()) return;
      this.KeyCode = e.KeyCode;
    }

    private void _keyBox_KeyUp(object sender, KeyEventArgs e) {
      e.Handled = true;
    }

    private void _keyBox_KeyPress(object sender, KeyPressEventArgs e) {
      e.Handled = true;
    }

    public void SetKeyCode(bool win, bool alt, bool ctrl, bool shift, Keys keyCode) {
      bool changed = (win != _win) || (alt != _alt) || (ctrl != _ctrl) || (shift != _shift) || (keyCode != _keyCode);
      if (!changed) return;
      _winBox.Checked = _win = win;
      _altBox.Checked = _alt = alt;
      _ctrlBox.Checked = _ctrl = ctrl;
      _shiftBox.Checked = _shift = shift;
      _keyCode = keyCode;
      _keyCodeBox.Text = keyCode.ToString();
      KeyCodeChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool Win {
      get => _win;
      set => SetKeyCode(value, _alt, _ctrl, _shift, _keyCode);
    }

    public bool Alt {
      get => _alt;
      set => SetKeyCode(_win, value, _ctrl, _shift, _keyCode);
    }

    public bool Ctrl {
      get => _ctrl;
      set => SetKeyCode(_win, _alt, value, _shift, _keyCode);
    }

    public bool Shift {
      get => _shift;
      set => SetKeyCode(_win, _alt, _ctrl, value, _keyCode);
    }

    public Keys KeyCode {
      get => _keyCode;
      set {
        var ignoreKeys = new Keys[] {
          Keys.Menu, Keys.ControlKey, Keys.ShiftKey, 
            Keys.Back, Keys.Delete, Keys.Escape
        };
        if (ignoreKeys.Contains(value)) {
          value = Keys.None;
        }

        SetKeyCode(_win, _alt, _ctrl, _shift, value);
      }
    }

    public void Dispose()
    {
      _x11KeyListener?.Dispose();
    }
  }

  sealed class X11KeyListener : IDisposable {
    private const int POLLING_T = 50; // ポーリング間隔 ms

    // --- X11 P/Invoke ---
    [DllImport("libX11.so.6")] static extern IntPtr XOpenDisplay(string dpy);
    [DllImport("libX11.so.6")] static extern int    XCloseDisplay(IntPtr d);
    [DllImport("libX11.so.6")] static extern int    XQueryKeymap(IntPtr d, byte[] keys);

    readonly KeyCodeBox _owner;
    readonly CancellationTokenSource _cts = new CancellationTokenSource();
    readonly IntPtr _display;
    readonly Task _task;

    byte[] _prev = new byte[32];

    volatile bool _active;

    public X11KeyListener(KeyCodeBox owner) {
      _owner = owner;
      _display = XOpenDisplay(null);
      if (_display == IntPtr.Zero) return; // X11 なし
      _active = false;

      _task = Task.Run(Loop, _cts.Token);
    }
    /// <summary>KeyCodeBox がフォーカスを得/失したときに呼ぶ</summary>
    public void SetActive(bool value) => _active = value;

    async Task Loop() {
      var map = new byte[32];
      while (!_cts.IsCancellationRequested) {
        if (!_active)
        {
          await Task.Delay(POLLING_T, _cts.Token).ConfigureAwait(false);
          continue;
        }
        XQueryKeymap(_display, map);

        // どの keycode が新たに押されたか調べる
        for (byte code = 8; code < 255; code++) { // X11 keycode 8-255
          int idx = code / 8, bit = code % 8;
          bool now = (map[idx] & (1 << bit)) != 0;
          bool was = (_prev[idx] & (1 << bit)) != 0;
          if (now && !was && TryTranslate(code, out Keys k)) {
            _owner.BeginInvoke(new Action(() => _owner.KeyCode = k));
            break; // 最初の押下だけで十分
          }
        }
        Buffer.BlockCopy(map, 0, _prev, 0, 32);
        await Task.Delay(POLLING_T, _cts.Token).ConfigureAwait(false);
      }
    }

    static bool TryTranslate(byte code, out Keys k) =>
      LinuxX11KeyMapper.TryX11KeycodeToKeys(code, out k);

    public void Dispose() {
      _cts.Cancel();
      try { _task?.Wait(200); } catch { /* ignore */ }
      if (_display != IntPtr.Zero) XCloseDisplay(_display);
    }
  }
}
