#!/usr/bin/env bash
# measure-mem.sh 定常状態メモリ消費比較スクリプト for Calctus.exe
#
# ・対象バイナリ : ../bin/Debug/Calctus.exe
# ・JIT-only     : mono -O=-aot
# ・App-only AOT : ./toggle-system-aot.sh off
# ・Full AOT     : ./toggle-system-aot.sh on
#

set -euo pipefail

BINARY="../bin/Debug/Calctus.exe"
TOGGLE="./toggle-system-aot.sh"
SMEM_CMD="smem -n"    # root 権限が必要です
memstat() {
  local label=$1; shift
  echo "=== $label ==="
  # キャッシュクリア
  sudo sh -c 'sync; echo 3 > /proc/sys/vm/drop_caches'
  # アプリ起動
  "$@" > /dev/null 2>&1 &
  local pid=$!
  # 安定待ち
  sleep 10

  local smem_out
  smem_out=$(sudo $SMEM_CMD "$pid")
  head -n1 <<< "$smem_out"
  #awk -v pid="$pid" '$1==pid { printf "PSS=%s KB, RSS=%s KB\n", $6, $7 }' <<< "$smem_out"
  grep -E "^$pid" <<< "$smem_out"
  echo ""

  # プロセス終了
  kill "$pid" >/dev/null 2>&1 || true
}

echo "Calctus.exe 定常時メモリ消費比較"
echo " - 起動して10秒後のメモリ使用状況"
echo

# 1) JIT-only
memstat "JIT-only" mono -O=-aot "$BINARY"

# 2) App-only AOT
echo "[*] Disabling library AOT modules..."
sudo "$TOGGLE" off > /dev/null
memstat "App-only AOT" mono "$BINARY"

# 3) Full AOT
echo "[*] Enabling all AOT modules..."
sudo "$TOGGLE" on > /dev/null
memstat "Full AOT" mono "$BINARY"

