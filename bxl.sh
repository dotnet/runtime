#!/bin/bash
# BuildXL wrapper — mirrors `bazel build` / `bazel test` subcommands.
#
# Usage:
#   ./bxl.sh build [extra-bxl-args...]   — compile everything (no test execution)
#   ./bxl.sh test  [extra-bxl-args...]   — compile and run tests
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")"; pwd -P)"

if [[ $# -eq 0 ]]; then
    echo "Usage: ./bxl.sh <command> [args...]" >&2
    echo "Commands:" >&2
    echo "  build   Compile all targets (excludes test/binary execution)" >&2
    echo "  test    Compile and run tests" >&2
    exit 1
fi

command="$1"
shift

case "$command" in
    build)
        filter_expression="~(tag='bxl-kind:binary')and~(tag='bxl-kind:test')"
        ;;
    test)
        filter_expression="tag='bxl-kind:test'"
        ;;
    *)
        echo "ERROR: unknown command '$command'. Use 'build' or 'test'." >&2
        exit 1
        ;;
esac

if [[ -n "${BXL_FILTER_APPEND:-}" ]]; then
    filter_expression="(${filter_expression})and(${BXL_FILTER_APPEND})"
fi

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
        rid_package="agtest.bxl.tool.$arch_dir"

        for store_root in "$bxl_dir/.store" "$HOME/.dotnet/tools/.store"; do
            [[ -d "$store_root" ]] || continue

            while IFS= read -r candidate; do
                if [[ -x "$candidate/bxl" && -d "$candidate/Sdk/Sdk.Transformers" ]]; then
                    BUILDXL_BIN="$candidate"
                    break 2
                fi
            done < <(find "$store_root" -type d -path "*/agtest.bxl.tool/*/$rid_package/*/tools/net*/$arch_dir" 2>/dev/null | sort -r)
        done
    fi
fi

if [[ -z "${BUILDXL_BIN:-}" ]]; then
    echo "ERROR: could not locate BUILDXL_BIN for '$BXL'." >&2
    exit 1
fi

export BUILDXL_BIN

cd "$script_dir"

exec "$BXL" \
    "/c:${script_dir}/config.dsc" \
    /server- \
    /unsafe_DisableDetours+ \
    /enableLinuxEBPFSandbox- \
    /cacheDirectory:"$CACHE_DIR" \
    /logOutput:FullOutputOnError \
    "/f:${filter_expression}" \
    "$@"
