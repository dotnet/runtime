#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail

runtime_root="$1"
framework_root="$2"
managed_assembly="$3"
output="$4"

if [[ -e "$output" ]]; then
    echo "Output directory already exists: $output" >&2
    exit 2
fi
for path in \
    "$runtime_root/corerun" \
    "$runtime_root/libcoreclr.so" \
    "$runtime_root/libclrgc.so" \
    "$runtime_root/System.Private.CoreLib.dll" \
    "$framework_root/System.Runtime.dll" \
    "$managed_assembly"; do
    [[ -f "$path" ]] || {
        echo "Runtime smoke input is missing: $path" >&2
        exit 3
    }
done

mkdir -p "$output"
summary="$output/runtime-smoke-summary.csv"
echo '"platform","linkage","gc_mode","pause_kinds","exit_code","marker","result"' >"$summary"

saved_core_libraries="${CORE_LIBRARIES-}"
saved_gc_path="${DOTNET_GCPath-}"
saved_gc_server="${DOTNET_gcServer-}"
trap 'export CORE_LIBRARIES="$saved_core_libraries"; export DOTNET_GCPath="$saved_gc_path"; export DOTNET_gcServer="$saved_gc_server"' EXIT

export CORE_LIBRARIES="$framework_root"
for linkage in linked standalone; do
    if [[ "$linkage" == linked ]]; then
        library="$runtime_root/libcoreclr.so"
        unset DOTNET_GCPath
    else
        library="$runtime_root/libclrgc.so"
        export DOTNET_GCPath="$library"
    fi

    for gc_mode in Workstation Server; do
        if [[ "$gc_mode" == Server ]]; then
            export DOTNET_gcServer=1
        else
            export DOTNET_gcServer=0
        fi
        label="$linkage-$gc_mode"
        log="$output/$label.log"
        set +e
        "$runtime_root/corerun" "$managed_assembly" "$library" "$label" >"$log" 2>&1
        exit_code=$?
        set -e
        marker="P3.2-PAUSE-SMOKE $label PASS"
        if [[ $exit_code -ne 0 ]] || ! grep -Fq "$marker" "$log"; then
            echo "Runtime smoke '$label' failed with exit code $exit_code." >&2
            exit 4
        fi
        echo "\"linux-x64\",\"$linkage\",\"$gc_mode\",\"Full;RefCount;InitialMark;FinalMark\",\"$exit_code\",\"$marker\",\"PASS\"" >>"$summary"
    done
done

echo "PASS: 4 P3.2 Linux runtime smoke rows"
