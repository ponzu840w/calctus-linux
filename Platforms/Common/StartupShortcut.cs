using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Shapoco.Platforms;

// Windows Script Host Object Model を参照設定すること
using Wsh = IWshRuntimeLibrary;

namespace Shapoco.Platforms.Common {
    public static class StartupShortcut {
        /// <summary>
        /// 現在の実行ファイルに対するショートカットがスタートアップフォルダに存在するか否かを返す
        /// </summary>
        public static bool CheckStartupRegistration() {
            return CheckStartupRegistration(out string[] dummy);
        }

        /// <summary>
        /// 現在の実行ファイルに対するショートカットがスタートアップフォルダに存在するか否かを返す
        /// </summary>
        public static bool CheckStartupRegistration(out string[] shortcutPath) {
            shortcutPath =
                FindShortcut(StartupPath, System.Windows.Forms.Application.ExecutablePath)
                .ToArray();
            return (shortcutPath.Length > 0);
        }

        /// <summary>
        /// 現在の実行ファイルに対するショートカットがスタートアップフォルダに存在するか否かを確認し、
        /// 存在しなければ作成する
        /// </summary>
        public static void SetStartupRegistration(
            bool registrationState,
            string defaultShortcutName = null,
            string arguments = null,
            string workDir = null) {
            var appPath = System.Windows.Forms.Application.ExecutablePath;

            if (string.IsNullOrEmpty(defaultShortcutName)) {
                var ext = Platform.IsMono() ? ".desktop" : ".lnk";
                defaultShortcutName = Path.GetFileNameWithoutExtension(appPath) + ext;
            }

            if (registrationState) {
                if (!CheckStartupRegistration()) {
                    // 作成する場合
                    var shortcutPath = Path.Combine(StartupPath, defaultShortcutName);

                    // ファイル名の衝突を回避する
                    int fileNumber = 1;
                    while (File.Exists(shortcutPath)) {
                        var baseName = Path.GetFileNameWithoutExtension(defaultShortcutName);
                        var ext = Path.GetExtension(defaultShortcutName);
                        shortcutPath = Path.Combine(StartupPath, baseName + (fileNumber++) + ext);
                    }

                    CreateShortcut(
                        shortcutPath: shortcutPath,
                        targetPath: appPath,
                        workDir: workDir,
                        arguments: arguments);
                }
            }
            else {
                // 削除する場合
                foreach (var shortcutPath in FindShortcut(StartupPath, appPath)) {
                    try {
                        File.Delete(shortcutPath);
#if DEBUG
                        Console.WriteLine($"Delete shortcut: {shortcutPath}");
#endif
                    }
                    catch (Exception) { }
                }
            }
        }

        /// <summary>
        /// ショートカットファイルを作成する
        /// </summary>
        public static void CreateShortcut(
            string shortcutPath,
            string targetPath,
            string workDir = null,
            string arguments = "",
            string iconLocation = null) {

          if (Platform.IsMono()) {
            Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath));
            var sb = new StringBuilder();
            sb.AppendLine("[Desktop Entry]");
            sb.AppendLine("Type=Application");
            sb.AppendLine($"Exec=mono \"{targetPath}\" {arguments}");
            if (!string.IsNullOrEmpty(workDir))  sb.AppendLine($"Path={workDir}");
            if (!string.IsNullOrEmpty(iconLocation)) sb.AppendLine($"Icon={iconLocation}");
            sb.AppendLine("X-GNOME-Autostart-enabled=true");
            // BOMなしでないと実行されない
            System.Text.Encoding enc = new System.Text.UTF8Encoding(false);
            File.WriteAllText(shortcutPath, sb.ToString(), enc);
#if DEBUG
            Console.WriteLine($"Create shortcut (linux): {shortcutPath}");
#endif
            return;
          }

            Wsh.WshShell shell = null;
            Wsh.IWshShortcut shortcut = null;
            try {
                shell = new Wsh.WshShell();
                shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;

                if (!string.IsNullOrEmpty(workDir)) shortcut.WorkingDirectory = workDir;
                if (!string.IsNullOrEmpty(arguments)) shortcut.Arguments = arguments;
                //shortcut.Hotkey = "Ctrl+Alt+Shift+F12";
                //shortcut.WindowStyle = 1;
                //shortcut.Description = "テストのアプリケーション";
                if (!string.IsNullOrEmpty(iconLocation)) shortcut.IconLocation = iconLocation;

                shortcut.Save();
#if DEBUG
                Console.WriteLine($"Create shortcut (windows): {shortcutPath}");
#endif
            }
            catch (Exception ex) { throw ex; }
            finally {
                if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
                if (shell != null) Marshal.FinalReleaseComObject(shell);
            }
        }

        /// <summary>
        /// 指定されたディレクトリから、指定されたファイルをターゲットとするショートカットファイルを検索する
        /// </summary>
        public static IEnumerable<string> FindShortcut(string searchDir, string targetPath) {
            // Linux
            if (Platform.IsMono()) {
              if (!Directory.Exists(searchDir))
                yield break;
              foreach (var file in Directory.GetFiles(searchDir, "*.desktop")) {
                string execLine = null;
                try {
                  execLine = File.ReadLines(file)
                    .FirstOrDefault(l => l.StartsWith("Exec=mono ", StringComparison.OrdinalIgnoreCase));
                } catch { continue; }
                if (execLine == null) continue;

                var execPath = execLine.Substring(10).Trim() .Split(' ').First() .Trim('"');

                if (execPath == targetPath)
                  yield return file;
              }
              yield break;
            }

            // Windows
            targetPath = targetPath.ToLower(); // 大小文字同一視のため小文字化

            var shell = new Wsh.IWshShell_Class();
            foreach (var linkFilePath in Directory.GetFiles(StartupPath)) {
                bool hit = false;
                Wsh.IWshShortcut_Class shortcut = null;
                try {
                    shortcut = (Wsh.IWshShortcut_Class)shell.CreateShortcut(linkFilePath);
                    hit = (shortcut.TargetPath.ToLower() == targetPath); // 小文字化して比較
                }
                catch (Exception) { }
                finally {
                    if (shortcut != null) Marshal.FinalReleaseComObject(shortcut);
                }

                // try-catch内ではyield returnできないので外で
                if (hit) yield return linkFilePath;
            }

            if (shell != null) Marshal.FinalReleaseComObject(shell);

        }

        /// <summary>
        /// スタートアップフォルダの場所
        /// </summary>
        public static string StartupPath {
            get {
              if (Platform.IsMono()) {
                // XDG 互換: ~/.config/autostart
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    ".config", "autostart");
              } else {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    "Startup");
              }
            }
        }

    }
}
