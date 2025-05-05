using System;

namespace Shapoco.Platforms.Common
{
    public static class WshFactory
    {
        // Lazy<T> を使って最初のアクセス時だけ COM オブジェクトを生成
        private static readonly Lazy<dynamic> _wshInstance = new Lazy<dynamic>(() =>
        {
            var progId = "WScript.Shell";
            var wshType = Type.GetTypeFromProgID(progId);
            if (wshType == null)
                throw new InvalidOperationException($"{progId} COM が見つかりません。");
            //var sh = (Shell32.IShellDispatch4)Activator.CreateInstance(
              //                         Type.GetTypeFromProgID("Shell.Application"));

            return Activator.CreateInstance(wshType);
        });

        /// <summary>
        /// WScript.Shell の dynamic インスタンスを返します。
        /// </summary>
        public static dynamic GetWsh()
        {
            return _wshInstance.Value;
        }
    }
}