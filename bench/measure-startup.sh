#!/usr/bin/env bash
# measure-calctus.sh GUI 起動ベンチマーク for Calctus.exe
#
# ・--bench で起動後すぐ落ちるモードを利用
# ・対象バイナリは ../bin/Debug/Calctus.exe
# ・JIT-only: -O=-aot
# ・アプリだけ AOT: toggle.sh off
# ・フル AOT   : toggle.sh on
#
set -euo pipefail

BINARY="../bin/Debug/Calctus.exe"

measure_gui() {
  mode=$1; shift
  bin=( "$@" )
  total=0
  for i in {1..5}; do
    sudo sh -c 'sync; echo 3 > /proc/sys/vm/drop_caches'
    # --bench を付けて、Shown 後に即 exit するモード
    ms=$("${bin[@]}" --bench 2>&1 | awk -F= '/STARTUP_MS/ {print $2}')
    total=$((total + ms))
  done
  avg=$((total / 5))
  printf "%-15s : %.1f ms\n" "$mode" "$avg"
}

echo "Calctus.exe 起動速度比較"
echo " - 5回起動して平均値を計算"
echo

# 1) JIT-only (AOT コードは無視)
measure_gui "JIT only" mono -O=-aot ${BINARY}

# 2) アプリ本体のみ AOT (ライブラリの .dll.so は無効化)
sudo ./toggle-system-aot.sh off > /dev/null
measure_gui "App-only AOT" mono ${BINARY}

# 3) フル AOT (ライブラリの .dll.so も有効化)
sudo ./toggle-system-aot.sh on > /dev/null
measure_gui "Full AOT" mono ${BINARY}
