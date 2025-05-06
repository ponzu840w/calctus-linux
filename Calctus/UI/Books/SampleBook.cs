using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Shapoco.Calctus.UI.Books {
    class SampleBook : Book {
        public const string SampleFolderName = "Samples";

        // サンプルフォルダがありそうな場所を探す
        private static bool _directorySearchDone = false;
        private static string _directorySearchResult = null;
        public static bool FindSampleFolder(out string path) {
            if (!_directorySearchDone) {
                try {
                  // 実行アセンブリのパス
                  string binPath = AppDataManager.AssemblyPath;

                  // 直接 Sample を探す
                  string candidate = Path.Combine(binPath, SampleFolderName);

                  // bin/Debug or bin/Release の判定用セグメント
                  string debugSeg   = Path.Combine("bin", "Debug");
                  string releaseSeg = Path.Combine("bin", "Release");

                  // プロジェクト直下に Sample があるケース
                  if (Directory.Exists(candidate)) { _directorySearchResult = candidate; }
                  // Debug or Release の場合、さらに一段上（プロジェクト直下）を探す
                  else if (binPath.EndsWith(debugSeg) || binPath.EndsWith(releaseSeg)) {
                    string projectDir  = Path.GetDirectoryName(Path.GetDirectoryName(binPath));
                    string fallback    = Path.Combine(projectDir, SampleFolderName);
                    if (Directory.Exists(fallback)) {
                      _directorySearchResult = fallback;
                    }
                  }

                }
                catch {
                    _directorySearchResult = null;
                }
                _directorySearchDone = true;
            }
            path = _directorySearchResult;
            return !string.IsNullOrEmpty(path);
        }

        public SampleBook() : base(SampleFolderName, SampleFolderName, SortMode.ByName) { }

        public override string DirectoryPath {
            get {
                if (FindSampleFolder(out var path)) {
                    return path;
                }
                else {
                    return null;
                }
            }
        }
    }
}
