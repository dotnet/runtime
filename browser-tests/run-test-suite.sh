#!/bin/bash

# Script to run a single browser test suite with CoreCLR
# Usage: ./browser-tests/run-test-suite.sh <SuiteName> <csprojPath>
# Example: ./browser-tests/run-test-suite.sh System.Resources.Writer.Tests src/libraries/System.Resources.Writer/tests/System.Resources.Writer.Tests.csproj
#
# Follows the process documented in test-suite.md

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

# Prepare results directory
RESULTS_DIR="$SCRIPT_DIR/results/$SUITE_NAME"
mkdir -p "$RESULTS_DIR"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BUILD_LOG="$RESULTS_DIR/build_${TIMESTAMP}.log"

echo "=========================================="
echo "Running: $SUITE_NAME"
echo "csproj:  $CSPROJ_PATH"
echo "Results: $RESULTS_DIR"
echo "=========================================="

# Download Mono baseline if not exists
"$SCRIPT_DIR/download-mono-baseline.sh" "$SUITE_NAME" || true

# Run tests and capture full output
echo ""
echo "Building and running tests..."
set +e  # Don't exit on test failure
dotnet build /t:test "$CSPROJ_PATH" -c Release -p:TargetOS=browser -p:TargetArchitecture=wasm 2>&1 | tee "$BUILD_LOG"
BUILD_EXIT=$?
set -e

# Extract exit code from xharness output (more reliable than build exit code)
XHARNESS_EXIT=$(grep -oP 'XHarness exit code: \K\d+' "$BUILD_LOG" | tail -1 || echo "")
if [ -z "$XHARNESS_EXIT" ]; then
    XHARNESS_EXIT=$BUILD_EXIT
fi

# Extract test summary
TEST_SUMMARY=$(grep "Tests run:" "$BUILD_LOG" | tail -1 || echo "No test summary found")

echo ""
echo "XHarness exit code: $XHARNESS_EXIT"
echo "$TEST_SUMMARY"

# Find and copy test results from artifacts
# Paths can vary: net11.0-browser/browser-wasm or net11.0/browser-wasm
RESULTS_COPIED=false
CONSOLE_COPIED=false

for pattern in \
    "$REPO_ROOT/artifacts/bin/$SUITE_NAME/Release/net*-browser/browser-wasm/wwwroot/xharness-output" \
    "$REPO_ROOT/artifacts/bin/$SUITE_NAME/Release/net*/browser-wasm/wwwroot/xharness-output"
do
    for dir in $pattern; do
        if [ -d "$dir" ]; then
            # Copy testResults.xml
            if [ -f "$dir/testResults.xml" ] && [ "$RESULTS_COPIED" = false ]; then
                cp "$dir/testResults.xml" "$RESULTS_DIR/testResults_${TIMESTAMP}.xml"
                echo "Copied: testResults_${TIMESTAMP}.xml"
                RESULTS_COPIED=true
            fi
            
            # Copy wasm-console.log (the console output from the test run)
            if [ -f "$dir/wasm-console.log" ] && [ "$CONSOLE_COPIED" = false ]; then
                cp "$dir/wasm-console.log" "$RESULTS_DIR/console_${TIMESTAMP}.log"
                echo "Copied: console_${TIMESTAMP}.log"
                CONSOLE_COPIED=true
            fi
            
            # Also check for browser-wasm.log (alternative name)
            if [ -f "$dir/browser-wasm.log" ] && [ "$CONSOLE_COPIED" = false ]; then
                cp "$dir/browser-wasm.log" "$RESULTS_DIR/console_${TIMESTAMP}.log"
                echo "Copied: console_${TIMESTAMP}.log (from browser-wasm.log)"
                CONSOLE_COPIED=true
            fi
        fi
    done
done

# Report what was copied
echo ""
if [ "$RESULTS_COPIED" = false ]; then
    echo "⚠️  Warning: Could not find testResults.xml"
fi
if [ "$CONSOLE_COPIED" = false ]; then
    echo "⚠️  Warning: Could not find console log (wasm-console.log or browser-wasm.log)"
fi

# Run comparison if Mono baseline exists
if [ -f "$RESULTS_DIR/mono-testResults.xml" ] && [ "$RESULTS_COPIED" = true ]; then
    echo ""
    echo "Running comparison..."
    "$SCRIPT_DIR/compare-test-results.sh" "$SUITE_NAME" 2>&1 | tail -20
fi

# Output final status
echo ""
echo "=========================================="
if [ "$XHARNESS_EXIT" = "0" ]; then
    echo "✅ $SUITE_NAME: PASSED"
    echo "=========================================="
else
    echo "❌ $SUITE_NAME: FAILED (exit code: $XHARNESS_EXIT)"
    echo "=========================================="
    echo ""
    echo "Next steps (per test-suite.md):"
    echo "  1. Check console log: $RESULTS_DIR/console_${TIMESTAMP}.log"
    echo "  2. Check test results: $RESULTS_DIR/testResults_${TIMESTAMP}.xml"
    echo "  3. For failures: Mark with [ActiveIssue(\"https://github.com/dotnet/runtime/issues/123011\")]"
    echo "  4. For timeouts: Find last running test in console log"
fi

exit ${XHARNESS_EXIT:-1}
