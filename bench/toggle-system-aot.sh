#!/usr/bin/env bash
#
# mono-aot-toggle.sh
# ────────────────────────────────────────────────────────────
# Toggle only “*.dll.so” files under /usr/local/lib
# between enabled (‐.dll.so) and disabled (‐.dll.so.disabled).
#
# Usage:
#   sudo ./mono-aot-toggle.sh off   # rename foo.dll.so → foo.dll.so.disabled
#   sudo ./mono-aot-toggle.sh on    # rename foo.dll.so.disabled → foo.dll.so
#
set -euo pipefail

usage() {
  echo "Usage: sudo $0 {off|on}"
  exit 1
}

[[ $# -eq 1 ]] || usage
action=$1
root="/usr/local/lib/mono"     # ← 必要ならここを書き換えてください
suffix=".disabled"

case "$action" in
  off)
    echo "[*] Disabling *.dll.so under $root …"
    find "$root" -type f -name '*.dll.so' ! -name "*$suffix" -print0 | \
    while IFS= read -r -d '' f; do
      mv -v "$f" "$f$suffix"
    done
    ;;

  on)
    echo "[*] Restoring *.dll.so under $root …"
    find "$root" -type f -name '*.dll.so'"$suffix" -print0 | \
    while IFS= read -r -d '' f; do
      orig="${f%$suffix}"
      if [[ -e "$orig" ]]; then
        echo "WARNING: $orig already exists; skipping restore of $f" >&2
      else
        mv -v "$f" "$orig"
      fi
    done
    ;;

  *)
    usage
    ;;
esac

echo "Done."
