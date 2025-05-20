using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Shapoco.Calctus
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            bool bench = args.Contains("--bench");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Model.Formats.NumberFormatter.Test();
            Model.Standards.PreferredNumbers.Test();
            Model.Types.ufixed113.Test();
            Model.Types.quad.Test();
            Model.Mathematics.QMath.Test();
            Model.Functions.BuiltInFuncDef.GenerateDocumentation();
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#if DEBUG
            var f = new UI.MainForm();
            f.Shown += (_, __) => {
              sw.Stop();
              Console.Error.WriteLine($"STARTUP_MS={sw.ElapsedMilliseconds}");
              if (bench) Application.ExitThread();   // ←ベンチ時は即終了
            };
            Application.Run(f);
#else
            Application.Run(new UI.MainForm());
#endif
            Settings.Instance.Save();
        }
    }
}
