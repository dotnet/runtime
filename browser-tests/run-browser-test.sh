#!/bin/bash

# Script to run a single Browser/WASM CoreCLR test suite
# Usage: ./browser-tests/run-browser-test.sh [-c Configuration] <path-to-test-csproj>
# Example: ./browser-tests/run-browser-test.sh src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System.Runtime.InteropServices.JavaScript.Tests.csproj
# Example: ./browser-tests/run-browser-test.sh -c Release src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System.Runtime.InteropServices.JavaScript.Tests.csproj

set -e

# Get the directory where this script is located and the repo root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Change to repo root for all operations
cd "$REPO_ROOT"

# Default configuration
CONFIGURATION="Debug"

# Parse optional arguments
while getopts "c:" opt; do
    case $opt in
        c)
            CONFIGURATION="$OPTARG"
            ;;
        \?)
            echo "Invalid option: -$OPTARG" >&2
            exit 1
            ;;
    esac
done
shift $((OPTIND-1))

if [ -z "$1" ]; then
    echo "Usage: $0 [-c Configuration] <path-to-test-csproj>"
    echo "  -c Configuration  Build configuration (default: Debug)"
    echo "Example: $0 src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System.Runtime.InteropServices.JavaScript.Tests.csproj"
    echo "Example: $0 -c Release src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System.Runtime.InteropServices.JavaScript.Tests.csproj"
    exit 1
fi

CSPROJ_PATH="$1"

if [ ! -f "$CSPROJ_PATH" ]; then
    echo "Error: File not found: $CSPROJ_PATH"
    exit 1
fi

# Extract test project name from csproj filename (without .csproj extension)
TEST_PROJECT_NAME=$(basename "$CSPROJ_PATH" .csproj)

# Set required environment variables
export RuntimeFlavor="CoreCLR"
export Scenario="WasmTestOnChrome"
export InstallFirefoxForTests="false"
export XunitShowProgress="true"

# Required for HTTPS test server (xharness uses HTTPS)
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"

# Create results directory (relative to repo root)
RESULTS_DIR="${REPO_ROOT}/browser-tests/results/${TEST_PROJECT_NAME}"
mkdir -p "$RESULTS_DIR"

# Timestamp for this run
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

echo "========================================"
echo "Running Browser/WASM CoreCLR Test Suite"
echo "========================================"
echo "Project:       $TEST_PROJECT_NAME"
echo "Csproj:        $CSPROJ_PATH"
echo "Configuration: $CONFIGURATION"
echo "Results:       $RESULTS_DIR"
echo "Time:          $TIMESTAMP"
echo "========================================"

# Run the test and capture output
# Using set +e to continue even if tests fail
# Using nohup to allow tests to continue if terminal disconnects
set +e

nohup ./dotnet.sh build -bl \
    /p:TargetOS=browser \
    /p:TargetArchitecture=wasm \
    /p:Configuration=${CONFIGURATION} \
    /t:Test \
    "$CSPROJ_PATH" 2>&1 | tee "${RESULTS_DIR}/console_${TIMESTAMP}.log"

TEST_EXIT_CODE=${PIPESTATUS[0]}

set -e

echo ""
echo "========================================"
echo "Test run completed with exit code: $TEST_EXIT_CODE"
echo "========================================"

# Find and copy xharness output files
# Pattern: artifacts/bin/<TestProjectName>/<Configuration>/net11.0-browser/browser-wasm/wwwroot/xharness-output/
XHARNESS_OUTPUT_DIR="${REPO_ROOT}/artifacts/bin/${TEST_PROJECT_NAME}/${CONFIGURATION}/net11.0-browser/browser-wasm/wwwroot/xharness-output"

if [ -d "$XHARNESS_OUTPUT_DIR" ]; then
    echo "Copying xharness output from: $XHARNESS_OUTPUT_DIR"
    
    if [ -f "${XHARNESS_OUTPUT_DIR}/testResults.xml" ]; then
        cp "${XHARNESS_OUTPUT_DIR}/testResults.xml" "${RESULTS_DIR}/testResults_${TIMESTAMP}.xml"
        echo "  - Copied testResults.xml"
    else
        echo "  - Warning: testResults.xml not found"
    fi
    
    if [ -f "${XHARNESS_OUTPUT_DIR}/wasm-console.log" ]; then
        cp "${XHARNESS_OUTPUT_DIR}/wasm-console.log" "${RESULTS_DIR}/wasm-console_${TIMESTAMP}.log"
        echo "  - Copied wasm-console.log"
    else
        echo "  - Warning: wasm-console.log not found"
    fi
else
    echo "Warning: xharness output directory not found: $XHARNESS_OUTPUT_DIR"
    echo "Looking for alternative locations..."
    
    # Try alternative path pattern (net11.0/browser-wasm instead of net11.0-browser/browser-wasm)
    ALT_XHARNESS_OUTPUT_DIR="${REPO_ROOT}/artifacts/bin/${TEST_PROJECT_NAME}/${CONFIGURATION}/net11.0/browser-wasm/wwwroot/xharness-output"
    
    if [ -d "$ALT_XHARNESS_OUTPUT_DIR" ]; then
        echo "Found alternative location: $ALT_XHARNESS_OUTPUT_DIR"
        
        if [ -f "${ALT_XHARNESS_OUTPUT_DIR}/testResults.xml" ]; then
            cp "${ALT_XHARNESS_OUTPUT_DIR}/testResults.xml" "${RESULTS_DIR}/testResults_${TIMESTAMP}.xml"
            echo "  - Copied testResults.xml"
        fi
        
        if [ -f "${ALT_XHARNESS_OUTPUT_DIR}/wasm-console.log" ]; then
            cp "${ALT_XHARNESS_OUTPUT_DIR}/wasm-console.log" "${RESULTS_DIR}/wasm-console_${TIMESTAMP}.log"
            echo "  - Copied wasm-console.log"
        fi
    else
        # Last resort: find and copy any testResults.xml for this project
        FOUND_RESULTS=$(find "${REPO_ROOT}/artifacts/bin" -name "testResults.xml" -path "*${TEST_PROJECT_NAME}*" 2>/dev/null | head -1)
        if [ -n "$FOUND_RESULTS" ]; then
            echo "Found: $FOUND_RESULTS"
            cp "$FOUND_RESULTS" "${RESULTS_DIR}/testResults_${TIMESTAMP}.xml"
            echo "  - Copied testResults.xml"
            
            # Try to find matching wasm-console.log
            FOUND_CONSOLE=$(dirname "$FOUND_RESULTS")/wasm-console.log
            if [ -f "$FOUND_CONSOLE" ]; then
                cp "$FOUND_CONSOLE" "${RESULTS_DIR}/wasm-console_${TIMESTAMP}.log"
                echo "  - Copied wasm-console.log"
            fi
        else
            echo "No testResults.xml found for ${TEST_PROJECT_NAME}"
        fi
    fi
fi

echo ""
echo "========================================"
echo "Results saved to: $RESULTS_DIR"
echo "========================================"
ls -la "$RESULTS_DIR"

echo ""
if [ $TEST_EXIT_CODE -ne 0 ]; then
    echo "⚠️  Tests failed or crashed (exit code: $TEST_EXIT_CODE)"
    echo ""
    echo "To analyze failures:"
    echo "  1. Check ${RESULTS_DIR}/wasm-console_${TIMESTAMP}.log for [FAIL] or last [STRT] entry"
    echo "  2. Check ${RESULTS_DIR}/testResults_${TIMESTAMP}.xml for detailed failure info"
    echo ""
    echo "If timeout/crash, find last running test:"
    echo "  grep '\\[STRT\\]' ${RESULTS_DIR}/wasm-console_${TIMESTAMP}.log | tail -1"
fi

exit $TEST_EXIT_CODE
