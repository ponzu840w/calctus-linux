using System;
using System.Windows.Forms;

namespace Shapoco.Platforms.Common {
    public interface IHotKeyService : IDisposable {
        /// <summary>ホットキー登録</summary>
        /// <returns>成功したら true</returns>
        bool Register(ModifierKey modifiers, Keys key);

        /// <summary>ホットキー解除</summary>
        void Unregister();

        /// <summary>ホットキー押下イベント</summary>
        event EventHandler HotKeyPressed;
    }
}