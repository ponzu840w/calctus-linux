using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Shapoco.Platforms.Common;
using Shapoco.Calctus.UI;

namespace Shapoco.Platforms.Linux
{
    /// <summary>
    /// メインウィンドウの出没をトグルする実装
    /// Toggle() が呼ばれるたびに最小化／復帰を切り替えます。
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
        /// メインフォームを最小化／復帰させる
        /// ・Visible かつ Minimized でなければ、トレイアイコンが表示中なら隠す（トレイ格納）、
        ///   そうでなければタスクバーへ最小化。
        /// ・それ以外は復帰：タスクバー表示／通常サイズ／アクティブ化。
        /// </summary>
        public void Toggle(object sender, EventArgs e)
        {
            // 現在フォームが表示中かつ最小化状態でない場合 -> 隠す or 最小化
            if (mainForm.Visible && mainForm.WindowState != FormWindowState.Minimized)
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
                // 復帰処理
                mainForm.ShowInTaskbar = true;
                mainForm.Show();
                mainForm.WindowState = FormWindowState.Normal;
                mainForm.Activate();
            }
        }
    }
}
