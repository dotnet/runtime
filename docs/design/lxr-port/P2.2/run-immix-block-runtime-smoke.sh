#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail

if [[ $# -ne 3 ]]; then
  echo "usage: $0 <linux-runtime-root> <managed-smoke-assembly> <output-root>" >&2
  exit 2
fi

runtime_root="$(cd "$1" && pwd)"
managed_assembly="$(realpath "$2")"
output_root="$3"
corerun="$runtime_root/corerun"
coreclr="$runtime_root/libcoreclr.so"
standalone="$runtime_root/libclrgc.so"

required=(
  "$corerun"
  "$coreclr"
  "$standalone"
  "$runtime_root/libclrjit.so"
  "$runtime_root/System.Private.CoreLib.dll"
  "$runtime_root/System.Runtime.dll"
  "$runtime_root/System.Console.dll"
  "$managed_assembly"
)
for path in "${required[@]}"; do
  [[ -f "$path" ]] || {
    echo "Linux RuntimeRoot is incomplete; missing '$path'." >&2
    exit 3
  }
done
file "$coreclr" | grep -Eq 'ELF 64-bit.*x86-64' || {
  echo "Linux runtime binary is not x86-64: '$coreclr'." >&2
  exit 3
}
file "$standalone" | grep -Eq 'ELF 64-bit.*x86-64' || {
  echo "Linux standalone GC binary is not x86-64: '$standalone'." >&2
  exit 3
}

mkdir -p "$output_root"
printf 'platform,linkage,gc_mode,exit_code,result,log\n' \
  > "$output_root/runtime-smoke-summary.csv"

saved_gc_path="${DOTNET_GCPath-}"
saved_gc_server="${DOTNET_gcServer-}"
saved_hook="${P22_NATIVE_HOOK_LIBRARY-}"
saved_ready_to_run="${DOTNET_ReadyToRun-}"
saved_tiered="${DOTNET_TieredCompilation-}"
restore_environment() {
  if [[ -n "$saved_gc_path" ]]; then export DOTNET_GCPath="$saved_gc_path"; else unset DOTNET_GCPath; fi
  if [[ -n "$saved_gc_server" ]]; then export DOTNET_gcServer="$saved_gc_server"; else unset DOTNET_gcServer; fi
  if [[ -n "$saved_hook" ]]; then export P22_NATIVE_HOOK_LIBRARY="$saved_hook"; else unset P22_NATIVE_HOOK_LIBRARY; fi
  if [[ -n "$saved_ready_to_run" ]]; then export DOTNET_ReadyToRun="$saved_ready_to_run"; else unset DOTNET_ReadyToRun; fi
  if [[ -n "$saved_tiered" ]]; then export DOTNET_TieredCompilation="$saved_tiered"; else unset DOTNET_TieredCompilation; fi
}
trap restore_environment EXIT

export DOTNET_ReadyToRun=0
export DOTNET_TieredCompilation=0
for linkage in linked standalone; do
  if [[ "$linkage" == standalone ]]; then
    export DOTNET_GCPath="$standalone"
    export P22_NATIVE_HOOK_LIBRARY="$standalone"
  else
    unset DOTNET_GCPath
    export P22_NATIVE_HOOK_LIBRARY="$coreclr"
  fi

  for gc_mode in Workstation Server; do
    if [[ "$gc_mode" == Server ]]; then export DOTNET_gcServer=1; else export DOTNET_gcServer=0; fi
    id="$linkage-$gc_mode"
    log="$output_root/$id.log"
    set +e
    "$corerun" "$managed_assembly" > "$log" 2>&1
    exit_code=$?
    set -e
    [[ $exit_code -eq 0 ]] && grep -q '^PASS:' "$log" || {
      echo "Linux runtime smoke failed: $id. See $log." >&2
      exit 4
    }
    printf 'linux-x64,%s,%s,%s,PASS,%s\n' \
      "$linkage" "$gc_mode" "$exit_code" "$(basename "$log")" \
      >> "$output_root/runtime-smoke-summary.csv"
  done
done

echo "PASS: 4 Linux linked/standalone and GC-mode scenarios"
echo "Output: $output_root"
