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

# Get the repo root (script is in src/tasks/WasmAppBuilder)
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../../.." && pwd)"

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

    echo "[$target_os] Scan path: $scan_path"
    echo "[$target_os] Output path: $output_dir"
    echo "Running generator for $target_os..."
    echo "./dotnet.sh build /t:RunGenerator /p:RuntimeFlavor=CoreCLR /p:TargetOS=$target_os /p:GeneratorOutputPath=$output_dir /p:AssembliesScanPath=$scan_path src/tasks/WasmAppBuilder/WasmAppBuilder.csproj"
    ./dotnet.sh build /t:RunGenerator /p:RuntimeFlavor=CoreCLR "/p:TargetOS=$target_os" "/p:GeneratorOutputPath=$output_dir" "/p:AssembliesScanPath=$scan_path" src/tasks/WasmAppBuilder/WasmAppBuilder.csproj
}

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
