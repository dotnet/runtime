#!/usr/bin/env bash

set -euo pipefail

# Default configuration
configuration="Release"
browser_scan_path_override=""
wasi_scan_path_override=""
target_os=""

usage="Usage: $0 [options]

Options:
  -c, --configuration <Checked|Debug|Release>  Build configuration (default: Release)
  -s, --scan-path <path>                        Override the default browser scan path
  -w, --wasi-scan-path <path>                   Override the default wasi scan path
  -t, --target-os <browser|wasi>                Regenerate only this flavor (default: both)
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
        -t|--target-os)
            target_os="$2"
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

target_os="$(echo "$target_os" | tr '[:upper:]' '[:lower:]')"
case "$target_os" in
    ""|browser|wasi)
        ;;
    *)
        echo "Error: Invalid target OS \"$target_os\". Must be browser or wasi."
        exit 1
        ;;
esac

# Get the repo root (script is in src/coreclr/vm/wasm)
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../../../.." && pwd)"

echo "Configuration: $configuration"
echo "Repo root: $repo_root"

cd "$repo_root"

# The scan paths, the crossgen2 lookup and the P/Invoke module list all live in the project next
# to this script, so they are not restated here and in the .cmd.
args=(
    build "$script_dir/generate-coreclr-helpers.proj"
    -t:GenerateCallHelpers
    "-p:Configuration=$configuration"
)

if [[ -n "$browser_scan_path_override" ]]; then
    args+=("-p:BrowserScanPath=$browser_scan_path_override")
fi

if [[ -n "$wasi_scan_path_override" ]]; then
    args+=("-p:WasiScanPath=$wasi_scan_path_override")
fi

if [[ -n "$target_os" ]]; then
    args+=("-p:CallHelperTargetOS=$target_os")
fi

./dotnet.sh "${args[@]}"

echo "Done!"
