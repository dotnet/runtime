#!/usr/bin/env bash
set -euo pipefail

runtime_root="$1"
managed_assembly="$2"
output_root="$3"

if [[ -e "$output_root" ]]; then
    echo "Output directory already exists: $output_root" >&2
    exit 2
fi

for path in \
    "$runtime_root/corerun" \
    "$runtime_root/libcoreclr.so" \
    "$runtime_root/libclrgc.so" \
    "$runtime_root/libclrjit.so" \
    "$runtime_root/System.Private.CoreLib.dll" \
    "$runtime_root/System.Runtime.dll" \
    "$runtime_root/System.Console.dll" \
    "$managed_assembly"; do
    [[ -f "$path" ]] || { echo "Runtime smoke input is missing: $path" >&2; exit 3; }
done

mkdir -p "$output_root"
printf 'platform,linkage,gc_mode,exit_code,marker,result\n' \
    >"$output_root/runtime-smoke-summary.csv"
for linkage in linked standalone; do
    if [[ "$linkage" == standalone ]]; then
        library="$runtime_root/libclrgc.so"
        export DOTNET_GCPath="$library"
    else
        library="$runtime_root/libcoreclr.so"
        unset DOTNET_GCPath || true
    fi
    for gc_mode in Workstation Server; do
        [[ "$gc_mode" == Server ]] && export DOTNET_gcServer=1 || export DOTNET_gcServer=0
        label="$linkage-$gc_mode"
        log="$output_root/$label.log"
        "$runtime_root/corerun" "$managed_assembly" "$library" "$label" >"$log" 2>&1
        marker="P3.4-COMPLETION-COUNTER-SMOKE $label PASS"
        grep -Fx "$marker" "$log" >/dev/null
        printf 'linux-x64,%s,%s,0,"%s",PASS\n' \
            "$linkage" "$gc_mode" "$marker" \
            >>"$output_root/runtime-smoke-summary.csv"
    done
done
unset DOTNET_GCPath DOTNET_gcServer || true
echo "PASS: 4 P3.4 Linux runtime smoke rows"
