#!/usr/bin/env bash
# Wrapper around sccache for macOS builds.
# sccache cannot parse -Xarch_<arch> flags that CMake generates for PCH
# includes, and also drops plain -include flags during compilation.
# We rewrite "-Xarch_<arch> -include<path>" to "-Xclang -include -Xclang <path>"
# which sccache passes through correctly to the clang frontend.

args=()
skip_xarch=false

for arg in "$@"; do
    if $skip_xarch; then
        skip_xarch=false
        if [[ "$arg" == -include* ]]; then
            # Rewrite -include<path> to -Xclang -include -Xclang <path>
            local_path="${arg#-include}"
            args+=("-Xclang" "-include" "-Xclang" "$local_path")
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
