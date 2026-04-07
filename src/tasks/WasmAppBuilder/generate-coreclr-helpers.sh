#!/usr/bin/env bash

set -euo pipefail

# Default configuration
configuration="Debug"
scan_path_override=""

usage="Usage: $0 [options]

Options:
  -c, --configuration <Checked|Debug|Release>  Build configuration (default: Debug)
  -s, --scan-path <path>                       Override the default scan path
  -h, --help                                   Show this help message"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        -c|--configuration)
            configuration="$2"
            shift 2
            ;;
        -s|--scan-path)
            scan_path_override="$2"
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

if [[ -n "$scan_path_override" ]]; then
    scan_path="$scan_path_override"
else
    scan_path="$repo_root/artifacts/bin/testhost/net11.0-browser-$configuration-wasm/shared/Microsoft.NETCore.App/11.0.0/"
fi

if [[ ! -d "$scan_path" ]]; then
    echo "Error: Scan path does not exist: $scan_path"
    echo "Please build the runtime first using: ./build.sh clr+libs -os browser -c $configuration"
    exit 1
fi

cd "$repo_root"
echo "Scan path: $scan_path"

# Run the generator - invoke directly without building a command string
echo "Running generator..."
echo "./dotnet.sh build /t:RunGenerator /p:RuntimeFlavor=CoreCLR /p:GeneratorOutputPath=$repo_root/src/coreclr/vm/wasm/ /p:AssembliesScanPath=$scan_path src/tasks/WasmAppBuilder/WasmAppBuilder.csproj"
./dotnet.sh build /t:RunGenerator /p:RuntimeFlavor=CoreCLR "/p:GeneratorOutputPath=$repo_root/src/coreclr/vm/wasm/" "/p:AssembliesScanPath=$scan_path" src/tasks/WasmAppBuilder/WasmAppBuilder.csproj

echo "Done!"
