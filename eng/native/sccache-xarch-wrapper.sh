#!/usr/bin/env bash
# Wrapper around sccache for macOS builds.
# sccache cannot parse -Xarch_<arch> flags that CMake generates for PCH
# includes. Rewrite them to explicit Clang frontend options that preserve
# use of the generated PCH while remaining parseable by sccache.

for arg in "$@"; do
    if [[ "$arg" == "-emit-pch" ]]; then
        exec "$@"
    fi
done

args=()
skip_xarch=false

for arg in "$@"; do
    if $skip_xarch; then
        skip_xarch=false
        if [[ "$arg" == -include?* ]]; then
            local_path="${arg#-include}"
            args+=(
                "-Xclang" "-include-pch" "-Xclang" "${local_path}.pch"
                "-Xclang" "-include" "-Xclang" "$local_path"
            )
        else
            args+=("$arg")
        fi
        continue
    fi
    if [[ "$arg" == -Xarch_* ]]; then
        skip_xarch=true
        continue
    fi
    args+=("$arg")
done

exec sccache "${args[@]}"
