﻿using Shapoco.Platforms.Common;
using System.Windows.Forms;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Shapoco.Platforms.Windows {
	/// <summary>
	/// WindowsHotKeyImplクラスの初期化時に指定する修飾キー
	/// </summary>
	public enum MOD_KEY : int
	{
		NONE = 0x0000,
		ALT = 0x0001,
		CONTROL = 0x0002,
		SHIFT = 0x0004,
		WIN = 0x0008,
	}

	class WindowsHotKeyService : IHotKeyService	{
		private WindowsHotKeyImpl _impl;
		public event EventHandler HotKeyPressed;

		public bool Register(ModifierKey m, Keys k) {
			_impl = new WindowsHotKeyImpl((MOD_KEY)m, k);
			_impl.HotKeyPush += (s, e) => HotKeyPressed?.Invoke(this, EventArgs.Empty);
			return true; // 必要なら失敗検出も
		}

		public void Unregister() {
			_impl?.Dispose();
			_impl = null;
		}

		public void Dispose() => Unregister();
	}

	// C#|グローバルホットキーを登録する – 貧脚レーサーのサボり日記
	// https://anis774.net/codevault/hotkey.html
	/// <summary>
	/// グローバルホットキーを登録するクラス。
	/// 使用後は必ずDisposeすること。
	/// </summary>
	public class WindowsHotKeyImpl : IDisposable
	{
		HotKeyForm form;
		/// <summary>
		/// ホットキーが押されると発生する。
		/// </summary>
		public event EventHandler HotKeyPush;

		/// <summary>
		/// ホットキーを指定して初期化する。
		/// 使用後は必ずDisposeすること。
		/// </summary>
		/// <param name="modKey">修飾キー</param>
		/// <param name="key">キー</param>
		public WindowsHotKeyImpl(MOD_KEY modKey, Keys key)
		{
			form = new HotKeyForm(modKey, key, raiseHotKeyPush);
		}

		private void raiseHotKeyPush()
		{
			if (HotKeyPush != null)
			{
				HotKeyPush(this, EventArgs.Empty);
			}
		}

		public void Dispose()
		{
			form.Dispose();
		}

		private class HotKeyForm : Form
		{
			[DllImport("user32.dll")]
			extern static int RegisterHotKey(IntPtr HWnd, int ID, MOD_KEY MOD_KEY, Keys KEY);

			[DllImport("user32.dll")]
			extern static int UnregisterHotKey(IntPtr HWnd, int ID);

			const int WM_HOTKEY = 0x0312;
			int id;
			ThreadStart proc;

			public HotKeyForm(MOD_KEY modKey, Keys key, ThreadStart proc)
			{
				this.proc = proc;
				bool success = false;
				for (int i = 0x0000; i <= 0xbfff; i++)
				{
					if (RegisterHotKey(this.Handle, i, modKey, key) != 0)
					{
						id = i;
						success = true;
						break;
					}
				}
				if (!success)
				{
					//MessageBox.Show("Hotkey register failed.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					Console.WriteLine("Hotkey register failed.");
				}
			}

			protected override void WndProc(ref Message m)
			{
				base.WndProc(ref m);

				if (m.Msg == WM_HOTKEY)
				{
					if ((int)m.WParam == id)
					{
						proc();
					}
				}
			}

			protected override void Dispose(bool disposing)
			{
				UnregisterHotKey(this.Handle, id);
				base.Dispose(disposing);
			}
		}
	}
}