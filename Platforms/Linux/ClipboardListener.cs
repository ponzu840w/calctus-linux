using System;
using System.Windows.Forms;
using System.Timers;

namespace Shapoco.Linux {
    public class ClipboardListener : IClipboardListener {
        private readonly Timer _timer = new Timer(500);
        private string _lastText = "";

        public event EventHandler ClipboardChanged = delegate { };

        public ClipboardListener() {
            _timer.Elapsed += (s,e) => {
                string txt = Clipboard.GetText();
                if (txt != _lastText) {
                    _lastText = txt;
                    ClipboardChanged(this, EventArgs.Empty);
                }
            };
            _timer.Start();
        }

        public void Refresh() {
            _lastText = Clipboard.GetText();
        }

        public void Dispose() {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
