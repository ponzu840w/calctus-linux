using System;
using System.Drawing;
using System.Windows.Forms;
using Shapoco.Platforms;

namespace Shapoco.Calctus.UI.Sheets {
  /// <summary>候補フォームの中央管理センター</summary>
  sealed class CandidateFormManager {
    private static readonly CandidateFormManager _inst = new CandidateFormManager();
    public static CandidateFormManager Inst => _inst;

    private InputCandidateForm _form;
    private CandidateFormManager() { }

    /// <summary>候補を表示または更新</summary>
    public void Show(Control owner,
                     IInputCandidateProvider provider,
                     Point screenPt) {
      if (_form == null || _form.IsDisposed) {
        // フォームの生成（一生に一度のハズ）
        _form = new InputCandidateForm(provider);
      } else {
        // プロバイダの更新
        _form.SetProvider(provider);
      }

      // 表示
      _form.Visible = true;

      // 配置
      if (Platform.IsMono()) {
        // Monoでは親座標系
        if (owner != _form.Parent) _form.Parent = owner;
        _form.Location = owner.PointToClient(screenPt);
        owner.Focus();
      } else {
        // Windowsでは画面座標系
        _form.Location = screenPt;
      }
    }

    /// <summary>候補フォームが可視状態かどうか</summary>
    public bool AreShown() {
      if (_form == null || _form.IsDisposed || _form.IsDisposed || !_form.Visible) {
        return false;
      } else {
        return true;
      }
    }

    /// <summary>候補フォームを非表示</summary>
    public void Hide() => _form?.Hide();

    /// <summary>候補フォームの検索キーを更新</summary>
    public void SetKey(string key) => _form?.SetKey(key);

    /// <summary>現在の選択を上へ移動</summary>
    public void SelectUp() => _form?.SelectUp();

    /// <summary>現在の選択を下へ移動</summary>
    public void SelectDown() => _form?.SelectDown();

    /// <summary>フォーカスを持っているか否か</summary>
    public bool ContainsFocus {
      get {
        if (_form?.ContainsFocus == true) return true;
        else return false;
      }
    }

    /// <summary>現在選択中のアイテムを取得</summary>
    public InputCandidate SelectedItem {
      get {
        if (_form == null) { return null; }
        return _form.SelectedItem;
      }
    }

  }
}
