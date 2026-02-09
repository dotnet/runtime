#!/usr/bin/env bash

__VersionFolder="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRoot="$(cd "$(dirname "$__VersionFolder")/../../"; pwd -P)"

for path in "${__VersionFolder}/"*{.h,.c}; do
    dest="$__RepoRoot/artifacts/obj/$(basename "$path")"
    if [[ ! -e "$dest" ]]; then
        cp "$path" "$dest"
    fi
done
