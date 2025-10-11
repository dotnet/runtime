#!/bin/bash
# Script to run FileStream buffering optimization benchmarks

# Change to the directory containing this script
cd "$(dirname "$0")"

# Get the repository root (5 levels up from this script)
REPO_ROOT="$(cd ../../../../.. && pwd)"

# Set up the dotnet path
export PATH="$REPO_ROOT/.dotnet:$PATH"

echo "Running FileStream Buffering Optimization Benchmark..."
echo "Repository root: $REPO_ROOT"
echo "Dotnet version: $(dotnet --version)"
echo ""

# Build in Release mode
echo "Building benchmark..."
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo ""
echo "Running benchmark (this may take several minutes)..."
echo ""

# Run the benchmark
dotnet run -c Release --no-build

echo ""
echo "Benchmark complete!"
