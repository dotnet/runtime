#!/bin/bash
# Build using BuildXL with a shared cache under ~/.cache/bxl
set -euo pipefail

default_bxl="$(command -v bxl || true)"
if [[ -z "$default_bxl" ]]; then
    default_bxl="$HOME/code/BuildXL/Out/BootStrap/Microsoft.BuildXL.linux-x64.0.1.0-20260501.5/bxl"
fi

BXL="${BXL:-$default_bxl}"
CACHE_DIR="${BXL_CACHE_DIR:-${XDG_CACHE_HOME:-$HOME/.cache}/bxl}"

if [[ -z "${BUILDXL_BIN:-}" ]]; then
    bxl_real="$(realpath "$BXL")"
    bxl_dir="$(dirname "$bxl_real")"

    if [[ -d "$bxl_dir/Sdk/Sdk.Transformers" ]]; then
        BUILDXL_BIN="$bxl_dir"
    else
        case "$(uname -s)" in
            Linux*)   arch_dir="linux-x64" ;;
            Darwin*)  arch_dir="osx-x64" ;;
            MINGW*|MSYS*|CYGWIN*) arch_dir="win-x64" ;;
            *) echo "ERROR: unsupported host OS: $(uname -s)" >&2; exit 1 ;;
        esac

        while IFS= read -r candidate; do
            if [[ -x "$candidate/bxl" && -d "$candidate/Sdk/Sdk.Transformers" ]]; then
                BUILDXL_BIN="$candidate"
                break
            fi
        done < <(find "$HOME/.dotnet/tools/.store" -type d -path "*/agtest.bxl.tool/*/tools/net*/$arch_dir" 2>/dev/null | sort -r)
    fi
fi

if [[ -z "${BUILDXL_BIN:-}" ]]; then
    echo "ERROR: could not locate BUILDXL_BIN for '$BXL'." >&2
    exit 1
fi

export BUILDXL_BIN

if [[ -z "${DOTNET_ROOT:-}" ]]; then
    dotnet_path="$(realpath "$(command -v dotnet)")"
    DOTNET_ROOT="$(dirname "$dotnet_path")"
fi

if [[ -z "${DOTNET_SDK_VERSION:-}" ]]; then
    DOTNET_SDK_VERSION="$(dotnet --version)"
fi

export DOTNET_ROOT
export DOTNET_SDK_VERSION

exec "$BXL" \
    /c:config.dsc \
    /server- \
    /unsafe_DisableDetours+ \
    /enableLinuxEBPFSandbox- \
    /cacheDirectory:"$CACHE_DIR" \
    /logOutput:FullOutputOnError \
    "$@"
