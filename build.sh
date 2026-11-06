#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
args=()
for arg in "$@"; do
  case "$arg" in
    --test) args+=(-Test) ;;
    *) printf 'Usage: ./build.sh [--test]\n' >&2; exit 2 ;;
  esac
done
if ! command -v powershell.exe >/dev/null 2>&1; then
  printf 'Build requires WSL with Windows PowerShell and .NET Framework 4.8.\n' >&2
  exit 1
fi
windows_root="$(wslpath -w "$project_dir")"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "${windows_root}\\Build.ps1" "${args[@]}"
