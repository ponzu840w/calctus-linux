using System;
using System.Windows.Forms;
using Shapoco.Platforms.Common;

namespace Shapoco.Platforms.Linux
{
    /// <summary>
    /// メインウィンドウの出没をトグルする実装
    /// Toggle() が呼ばれるたびにアクティブ状態なら最小化、そうでなければ復帰・アクティブ化
    /// </summary>
    public class LinuxWindowPopupToggle : IWindowPopupToggle
    {
        private readonly Form mainForm;
        private readonly NotifyIcon trayIcon;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="mainForm">対象のメインフォーム</param>
        /// <param name="trayIcon">トレイアイコン（Visible フラグで動作モードを切り替え）</param>
        public LinuxWindowPopupToggle(Form mainForm, NotifyIcon trayIcon)
        {
            this.mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            this.trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));
        }

        /// <summary>
        /// メインフォームをアクティブ状態なら最小化、そうでなければ復帰・アクティブ化
        /// </summary>
        public void Toggle(object sender, EventArgs e)
        {
            // アプリがアクティブなら最小化／非表示
            if (Form.ActiveForm == mainForm)
            {
                if (trayIcon.Visible)
                {
                    // トレイ格納
                    mainForm.ShowInTaskbar = false;
                    mainForm.Hide();
                }
                else
                {
                    // タスクバー最小化
                    mainForm.ShowInTaskbar = true;
                    mainForm.WindowState = FormWindowState.Minimized;
                }
            }
            else
            {
                // アクティブでなければ復帰／最前面
                mainForm.ShowInTaskbar = true;
                mainForm.Show();
                mainForm.WindowState = FormWindowState.Normal;
                mainForm.Activate();
            }
        }
    }
}
