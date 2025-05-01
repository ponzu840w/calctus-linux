using System;
using System.IO;

namespace Shapoco.Linux {
    public class StartupRegistrar : IStartupRegistrar {
        private readonly string _autostartDir =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "autostart");
        private readonly string _desktopFile;

        public StartupRegistrar() {
            Directory.CreateDirectory(_autostartDir);
            _desktopFile = Path.Combine(_autostartDir,
                Path.GetFileNameWithoutExtension(
                  Application.ExecutablePath) + ".desktop");
        }

        public bool IsRegistered() => File.Exists(_desktopFile);

        public void SetRegistration(bool enable) {
            if (enable) {
                var content = $@"
[Desktop Entry]
Type=Application
Exec={Application.ExecutablePath}
Hidden=false
X-GNOME-Autostart-enabled=true
Name=MyApp
Comment=自動起動の設定
";
                File.WriteAllText(_desktopFile, content.Trim());
            }
            else if (File.Exists(_desktopFile)) {
                File.Delete(_desktopFile);
            }
        }
    }
}
