#!/usr/bin/env bash

set -euo pipefail

# Default configuration
configuration="Debug"
browser_scan_path_override=""
wasi_scan_path_override=""

usage="Usage: $0 [options]

Options:
  -c, --configuration <Checked|Debug|Release>  Build configuration (default: Debug)
  -s, --scan-path <path>                        Override the default browser scan path
  -w, --wasi-scan-path <path>                   Override the default wasi scan path
  -h, --help                                    Show this help message"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        -c|--configuration)
            configuration="$2"
            shift 2
            ;;
        -s|--scan-path)
            browser_scan_path_override="$2"
            shift 2
            ;;
        -w|--wasi-scan-path)
            wasi_scan_path_override="$2"
            shift 2
            ;;
        -h|--help)
            echo "$usage"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "$usage"
            exit 1
            ;;
    esac
done

# Validate configuration to prevent injection (case-insensitive)
config_lower="$(echo "$configuration" | tr '[:upper:]' '[:lower:]')"
case "$config_lower" in
    debug|release|checked)
        ;;
    *)
        echo "Error: Invalid configuration \"$configuration\". Must be Debug, Release, or Checked."
        exit 1
        ;;
esac

# Get the repo root (script is in src/coreclr/vm/wasm)
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../../../.." && pwd)"

echo "Configuration: $configuration"
echo "Repo root: $repo_root"

cd "$repo_root"

# Run the generator for a given target OS.
# Arguments: <target_os> <scan_path> <output_dir>
run_generator() {
    local target_os="$1"
    local scan_path="$2"
    local output_dir="$3"

    if [[ ! -d "$scan_path" ]]; then
        echo "Error: Scan path for $target_os does not exist: $scan_path"
        echo "Please build the runtime first using: ./build.sh clr+libs -os $target_os -c $configuration"
        exit 1
    fi

    if [[ ! -f "$crossgen2" ]]; then
        echo "Error: crossgen2 was not found at: $crossgen2"
        echo "Please build the clr subset first using: ./build.sh clr -c $configuration"
        exit 1
    fi

    echo "[$target_os] Scan path: $scan_path"
    echo "[$target_os] Output path: $output_dir"
    echo "Running generator for $target_os..."

    local args=(
        --targetos "$target_os"
        --targetarch wasm
        --generate-portable-callhelpers "$output_dir"
        --no-warn-unresolved-directpinvoke
    )
    local module
    for module in "${pinvoke_modules[@]}"; do
        args+=(--directpinvoke "$module")
    done

    ./dotnet.sh "$crossgen2" "${args[@]}" "$scan_path"*.dll
}

# Modules the runtime links statically; a P/Invoke into any of them resolves to a direct call.
pinvoke_modules=(
    libSystem.Native
    libSystem.Native.Browser
    libSystem.IO.Compression.Native
    libSystem.Globalization.Native
    libSystem.Runtime.InteropServices.JavaScript.Native
)

# The generator lives in crossgen2 and uses its type system to compute the wasm ABI. Generation
# does not load the JIT, so the host-targeting crossgen2 answers wasm questions correctly. Its
# configuration has to match the one the scanned assemblies came from.
crossgen2="$repo_root/artifacts/bin/coreclr/$(uname -s | tr '[:upper:]' '[:lower:]').$(uname -m).$configuration/crossgen2/crossgen2.dll"
case "$(uname -s)" in
    Darwin) crossgen2="${crossgen2/darwin./osx.}" ;;
esac
crossgen2="${crossgen2/aarch64./arm64.}"
crossgen2="${crossgen2/x86_64./x64.}"

# Resolve scan paths (allow overrides).
if [[ -n "$browser_scan_path_override" ]]; then
    browser_scan_path="$browser_scan_path_override"
else
    browser_scan_path="$repo_root/artifacts/bin/testhost/net11.0-browser-$configuration-wasm/shared/Microsoft.NETCore.App/11.0.0/"
fi

if [[ -n "$wasi_scan_path_override" ]]; then
    wasi_scan_path="$wasi_scan_path_override"
else
    wasi_scan_path="$repo_root/artifacts/bin/testhost/net11.0-wasi-$configuration-wasm/shared/Microsoft.NETCore.App/11.0.0/"
fi

run_generator "browser" "$browser_scan_path" "$repo_root/src/coreclr/vm/wasm/browser/"
run_generator "wasi" "$wasi_scan_path" "$repo_root/src/coreclr/vm/wasm/wasi/"

echo "Done!"
