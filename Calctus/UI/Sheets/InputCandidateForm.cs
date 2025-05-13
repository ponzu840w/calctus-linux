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
        /// 入力キーに応じて候補を更新する（高速版）
        /// </summary>
        public void SetKey(string key) {
          // 前回の選択ラベルを保持（無ければ null）
          var lastLabel = _list.SelectedItem is InputCandidate ic ? ic.Label : null;

          // 空文字対策（string.IndexOf は空文字で常に 0 を返すため）
          key ??= string.Empty;

          // 低コスト比較のため前処理
          var keyLower = key.ToLowerInvariant();

          // 候補抽出 & スコア付け（1 パス）
          var scored = new List<(InputCandidate cand, int score)>();
          foreach (var c in _provider.Candidates) {
            int score = -1;

            // キー長 0 なら全件表示（starts-with と同等の優先順位）
            if (key.Length == 0) score = 1;
            else if (c.Id.Equals(key, StringComparison.OrdinalIgnoreCase))           score = 0;
            else if (c.Id.StartsWith(key, StringComparison.OrdinalIgnoreCase))       score = 1;
            else if (c.Id.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)     score = 2;
            else if (!string.IsNullOrEmpty(c.Description) &&
                c.Description.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) score = 3;

            if (score >= 0) scored.Add((c, score));
          }

          // スコア → Id アルファベット順で安定ソート
          scored.Sort((a, b) => {
              int cmp = a.score.CompareTo(b.score);
              return cmp != 0 ? cmp
              : string.Compare(a.cand.Id, b.cand.Id, StringComparison.OrdinalIgnoreCase);
              });

          int selIndex = -1;

          // 一括で ListBox へ流し込み（再描画 OFF）
          _list.BeginUpdate();
          try {
            _list.Items.Clear();

            for (int i = 0; i < scored.Count; i++) {
              var cand = scored[i].cand;
              _list.Items.Add(cand);

              // 選択候補決定
              if (selIndex < 0) {
                if (cand.Id.Equals(key, StringComparison.OrdinalIgnoreCase)) selIndex = i;  // 完全一致
                else if (cand.Label == lastLabel)                                           // 以前の選択が残っている
                  selIndex = i;
              }
            }
          }
          finally { _list.EndUpdate(); }

          // フォールバック：何も決まっていなければ先頭を選ぶ
          if (selIndex < 0 && _list.Items.Count > 0) selIndex = 0;

          // 選択適用
          _list.SelectedIndex = selIndex;
          _desc.Text = selIndex >= 0
            ? ((InputCandidate)_list.Items[selIndex]).Description
            : string.Empty;
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

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                _list.Dispose();
                _desc.Dispose();
            }
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
