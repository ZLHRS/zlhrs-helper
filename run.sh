#!/usr/bin/env bash
set -euo pipefail
project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
if [[ ! -f "$project_dir/build/NVIDIAHelper.exe" ]]; then
  printf 'Application is missing. Pull the complete repository or run ./build.sh.\n' >&2
  exit 1
fi
if [[ ! -e /proc/sys/fs/binfmt_misc/WSLInterop ]]; then
  printf 'This Windows application requires Windows or WSL with Windows interop enabled.\n' >&2
  exit 1
fi
exec "$project_dir/build/NVIDIAHelper.exe" "$@"
