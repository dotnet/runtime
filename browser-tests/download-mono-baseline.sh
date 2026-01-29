#!/bin/bash

# Script to download Mono baseline console log from Helix
# Usage: ./browser-tests/download-mono-baseline.sh <TestProjectName>
# Example: ./browser-tests/download-mono-baseline.sh System.Runtime.InteropServices.JavaScript.Tests

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

if [ -z "$1" ]; then
    echo "Usage: $0 <TestProjectName>"
    echo "Example: $0 System.Runtime.InteropServices.JavaScript.Tests"
    exit 1
fi

TEST_PROJECT_NAME="$1"
RESULTS_DIR="${REPO_ROOT}/browser-tests/results/${TEST_PROJECT_NAME}"
MONO_LOG_PATH="${RESULTS_DIR}/mono-console.log"
WORKITEMS_JSON="${SCRIPT_DIR}/Mono-chrome-workitems.json"

# Check if already downloaded
if [ -f "$MONO_LOG_PATH" ]; then
    echo "Mono baseline already exists: $MONO_LOG_PATH"
    echo "Delete it first if you want to re-download."
    exit 0
fi

# Check workitems file exists
if [ ! -f "$WORKITEMS_JSON" ]; then
    echo "Error: Workitems file not found: $WORKITEMS_JSON"
    exit 1
fi

# Create results directory
mkdir -p "$RESULTS_DIR"

# Find the workitem for this test suite
WORKITEM_NAME="WasmTestOnChrome-ST-${TEST_PROJECT_NAME}"
echo "Looking for workitem: $WORKITEM_NAME"

DETAILS_URL=$(jq -r ".[] | select(.Name == \"$WORKITEM_NAME\") | .DetailsUrl" "$WORKITEMS_JSON" 2>/dev/null || echo "")

if [ -z "$DETAILS_URL" ] || [ "$DETAILS_URL" = "null" ]; then
    echo "Error: Workitem '$WORKITEM_NAME' not found in $WORKITEMS_JSON"
    echo ""
    echo "Available workitems containing '$TEST_PROJECT_NAME':"
    jq -r ".[].Name" "$WORKITEMS_JSON" | grep -i "$TEST_PROJECT_NAME" || echo "  (none found)"
    exit 1
fi

echo "Fetching workitem details from Helix API..."
WORKITEM_DETAILS=$(curl -s "$DETAILS_URL" 2>/dev/null)

if [ -z "$WORKITEM_DETAILS" ]; then
    echo "Error: Failed to fetch workitem details from: $DETAILS_URL"
    exit 1
fi

CONSOLE_URI=$(echo "$WORKITEM_DETAILS" | jq -r '.ConsoleOutputUri // empty' 2>/dev/null)

if [ -z "$CONSOLE_URI" ]; then
    echo "Error: ConsoleOutputUri not found in workitem details"
    echo "Response: $WORKITEM_DETAILS"
    exit 1
fi

echo "Downloading console log..."
if curl -s -o "$MONO_LOG_PATH" "$CONSOLE_URI"; then
    FILE_SIZE=$(wc -c < "$MONO_LOG_PATH")
    echo "✓ Downloaded Mono baseline: $MONO_LOG_PATH ($FILE_SIZE bytes)"
    
    # Extract and display test summary
    echo ""
    echo "Mono Test Summary:"
    grep "TEST EXECUTION SUMMARY" -A1 "$MONO_LOG_PATH" | tail -2 || echo "  (summary not found)"
else
    echo "Error: Failed to download console log from: $CONSOLE_URI"
    exit 1
fi
