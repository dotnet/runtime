#!/bin/bash

# Script to run a single browser test suite with CoreCLR
# Usage: ./browser-tests/run-test-suite.sh <SuiteName> <csprojPath>
# Example: ./browser-tests/run-test-suite.sh System.Resources.Writer.Tests src/libraries/System.Resources.Writer/tests/System.Resources.Writer.Tests.csproj

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

SUITE_NAME="$1"
CSPROJ_PATH="$2"

if [ -z "$SUITE_NAME" ] || [ -z "$CSPROJ_PATH" ]; then
    echo "Usage: $0 <SuiteName> <csprojPath>"
    echo "Example: $0 System.Resources.Writer.Tests src/libraries/System.Resources.Writer/tests/System.Resources.Writer.Tests.csproj"
    exit 1
fi

cd "$REPO_ROOT"

# Set environment for CoreCLR Browser/WASM testing
export RuntimeFlavor="CoreCLR"
export Scenario="WasmTestOnChrome"
export InstallFirefoxForTests="false"
export XunitShowProgress="true"
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"

echo "=========================================="
echo "Running: $SUITE_NAME"
echo "csproj:  $CSPROJ_PATH"
echo "=========================================="

# Download Mono baseline if not exists
"$SCRIPT_DIR/download-mono-baseline.sh" "$SUITE_NAME" || true

# Run tests
echo ""
echo "Building and running tests..."
dotnet build /t:test "$CSPROJ_PATH" -c Release -p:TargetOS=browser -p:TargetArchitecture=wasm 2>&1 | tee /tmp/test-output-$$.log | tail -60

# Extract exit code from xharness output
XHARNESS_EXIT=$(grep -oP 'XHarness exit code: \K\d+' /tmp/test-output-$$.log | tail -1 || echo "unknown")
TEST_SUMMARY=$(grep -A2 "TEST EXECUTION SUMMARY" /tmp/test-output-$$.log | tail -2 || echo "")

echo ""
echo "XHarness exit code: $XHARNESS_EXIT"
echo "$TEST_SUMMARY"

# Copy results to results directory
RESULTS_DIR="$SCRIPT_DIR/results/$SUITE_NAME"
mkdir -p "$RESULTS_DIR"

# Find and copy test results
RESULTS_COPIED=false
for dir in "$REPO_ROOT"/artifacts/bin/"$SUITE_NAME"/Release/net*/browser-wasm/wwwroot/xharness-output; do
    if [ -f "$dir/testResults.xml" ]; then
        TIMESTAMP=$(date +%Y%m%d_%H%M%S)
        cp "$dir/testResults.xml" "$RESULTS_DIR/testResults_${TIMESTAMP}.xml"
        echo "Results copied to: $RESULTS_DIR/testResults_${TIMESTAMP}.xml"
        RESULTS_COPIED=true
        break
    fi
done

if [ "$RESULTS_COPIED" = false ]; then
    echo "Warning: Could not find test results to copy"
fi

# Run comparison if Mono baseline exists
if [ -f "$RESULTS_DIR/mono-testResults.xml" ]; then
    echo ""
    echo "Running comparison..."
    "$SCRIPT_DIR/compare-test-results.sh" "$SUITE_NAME" 2>&1 | tail -20
fi

# Output final status
echo ""
echo "=========================================="
if [ "$XHARNESS_EXIT" = "0" ]; then
    echo "✅ $SUITE_NAME: PASSED"
else
    echo "❌ $SUITE_NAME: FAILED (exit code: $XHARNESS_EXIT)"
fi
echo "=========================================="

rm -f /tmp/test-output-$$.log

exit ${XHARNESS_EXIT:-1}
