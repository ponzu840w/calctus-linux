using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Shapoco.Platforms;

namespace Shapoco.Calctus.UI.Sheets {
    class InputCandidateForm : Form {
        private IInputCandidateProvider _provider;
        private ListBox _list = new ListBox();
        private Label _desc = new Label();

        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x40000000; // WS_CHILD
                cp.ExStyle |= 0x08000080; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        public InputCandidateForm(IInputCandidateProvider provider) {
            _provider = provider;

            FormBorderStyle = FormBorderStyle.None;

            // Windowsでは外部ウィンドウとして、monoでは内部フォームとして表示
            if(Platform.IsWindows()){ TopMost = true; }
            if(Platform.IsMono()){ TopLevel = false; }
            Size = new Size(250, 250);
            Font = new Font(Settings.Instance.Appearance_Font_Button_Name, Settings.Instance.Appearance_Font_Size, FontStyle.Regular);
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.White;
            Padding = new Padding(1, 1, 1, 1);
            DoubleBuffered = true;

            _list.Dock = DockStyle.Fill;
            _list.BorderStyle = BorderStyle.None;
            _list.IntegralHeight = false;
            _list.SelectedIndexChanged += _list_SelectedIndexChanged;
            Controls.Add(_list);

            _desc.Dock = DockStyle.Bottom;
            _desc.AutoSize = false;
            _desc.Height = 100;
            _desc.BackColor = Color.FromArgb(48, 48, 48);
            Controls.Add(_desc);

            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
        }

        /// <summary>
        /// 入力キーに応じて候補を更新する
        /// </summary>
        public void SetKey(string key) {
          // 前回の選択ラベルを保持
          var lastLabel = _list.SelectedItem is InputCandidate ic ? ic.Label : null;
          if (key is null) key = string.Empty;

          // ３つのバケットに振り分け
          var bucket1 = new List<InputCandidate>();  // 先頭一致 or key=="" の全件
          var bucket2 = new List<InputCandidate>();  // 部分一致
          var bucket3 = new List<InputCandidate>();  // 説明文一致

          foreach (var c in _provider.Candidates) {
            if (key.Length == 0 || c.Id.StartsWith(key, StringComparison.OrdinalIgnoreCase)) {
              bucket1.Add(c);
            }
            else if (c.Id.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) {
              bucket2.Add(c);
            }
            else if (!string.IsNullOrEmpty(c.Description)
                     && c.Description.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) {
              bucket3.Add(c);
            }
          }

          // ListBox に一気に追加しつつ、先頭一致グループだけで selIndex を決定
          _list.BeginUpdate();
          try {
            _list.Items.Clear();
            int selIndex = 0;
            int idx = 0;

            // --- グループ1: 先頭一致（or key=="" で全件） ---
            foreach (var c in bucket1) {
              _list.Items.Add(c);
              // 完全一致 or lastLabel があれば常に上書き
              if (c.Id.Equals(key, StringComparison.OrdinalIgnoreCase)
                  || c.Label == lastLabel) {
                selIndex = idx;
              }
              idx++;
            }
            // --- グループ2: 部分一致 ---
            foreach (var c in bucket2) {
              _list.Items.Add(c);
              idx++;
            }
            // --- グループ3: 説明一致 ---
            foreach (var c in bucket3) {
              _list.Items.Add(c);
              idx++;
            }

            // 選択項目の表示
            if (_list.Items.Count > 0) {
              _list.SelectedIndex = selIndex;
              _desc.Text = ((InputCandidate)_list.Items[selIndex]).Description;
            } else {
              _desc.Text = string.Empty;
            }
          } finally {
            _list.EndUpdate();
          }
        }

        public InputCandidate SelectedItem {
            get {
                if (_list.SelectedIndex >= 0) {
                    return (InputCandidate)_list.Items[_list.SelectedIndex];
                }
                else {
                    return null;
                }
            }
        }

        public void SelectUp() {
            if (_list.Items.Count == 0) return;
            if (_list.SelectedIndex >= 0) {
                _list.SelectedIndex = (_list.SelectedIndex + _list.Items.Count - 1) % _list.Items.Count;
            }
            else {
                _list.SelectedIndex = _list.Items.Count - 1;
            }
        }

        public void SelectDown() {
            if (_list.Items.Count == 0) return;
            if (_list.SelectedIndex >= 0) {
                _list.SelectedIndex = (_list.SelectedIndex + 1) % _list.Items.Count;
            }
            else {
                _list.SelectedIndex = 0;
            }
        }

        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            int fontSize = (int)Font.Size;
            Size = new Size(fontSize * 20, fontSize * 20);
            _desc.Height = fontSize * 5;
        }

        protected override void OnBackColorChanged(EventArgs e) {
            base.OnBackColorChanged(e);
            _list.BackColor = this.BackColor;
        }

        protected override void OnForeColorChanged(EventArgs e) {
            base.OnForeColorChanged(e);
            _list.ForeColor = this.ForeColor;
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            var g = e.Graphics;
            g.DrawRectangle(Pens.Gray, new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1));
            g.DrawLine(Pens.Gray, 0, _list.Bottom + 10, ClientSize.Width, _list.Bottom - 10);
        }

        public void SetProvider(IInputCandidateProvider provider) {
            _provider = provider;
        }

        private void _list_SelectedIndexChanged(object sender, EventArgs e) {
            if (_list.SelectedIndex >= 0) {
                var c = (InputCandidate)_list.Items[_list.SelectedIndex];
                _desc.Text = c.Description;
            }
            else {
                _desc.Text = "";
            }
        }

    }
}
