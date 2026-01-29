#!/bin/bash

# Script to compare test results between CoreCLR and Mono baseline
# Usage: ./browser-tests/compare-test-results.sh <TestProjectName>
# Example: ./browser-tests/compare-test-results.sh System.Runtime.InteropServices.JavaScript.Tests

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
MONO_RESULTS="${RESULTS_DIR}/mono-testResults.xml"

# Find the most recent CoreCLR test results
CORECLR_RESULTS=$(ls -t "${RESULTS_DIR}"/testResults_*.xml 2>/dev/null | head -1)

if [ ! -f "$MONO_RESULTS" ]; then
    echo "Error: Mono baseline not found: $MONO_RESULTS"
    echo "Run: ./browser-tests/download-mono-baseline.sh $TEST_PROJECT_NAME"
    exit 1
fi

if [ -z "$CORECLR_RESULTS" ] || [ ! -f "$CORECLR_RESULTS" ]; then
    echo "Error: CoreCLR test results not found in: $RESULTS_DIR"
    echo "Run the tests first with: ./browser-tests/run-browser-test.sh <path-to-csproj>"
    exit 1
fi

echo "Comparing test results:"
echo "  Mono:    $MONO_RESULTS"
echo "  CoreCLR: $CORECLR_RESULTS"
echo ""

# Create temp directory for intermediate files
TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

# Extract test names from XML files
# The test name is in the 'name' attribute of <test> elements
extract_test_names() {
    local xml_file="$1"
    grep -oP 'name="[^"]*"' "$xml_file" | \
        grep -v 'assembly name=' | \
        grep -v 'collection.*name=' | \
        sed 's/name="//;s/"$//' | \
        sort -u
}

echo "Extracting test names..."
extract_test_names "$MONO_RESULTS" > "$TEMP_DIR/mono-tests.txt"
extract_test_names "$CORECLR_RESULTS" > "$TEMP_DIR/coreclr-tests.txt"

MONO_COUNT=$(wc -l < "$TEMP_DIR/mono-tests.txt")
CORECLR_COUNT=$(wc -l < "$TEMP_DIR/coreclr-tests.txt")

echo "  Mono tests:    $MONO_COUNT"
echo "  CoreCLR tests: $CORECLR_COUNT"
echo ""

# Use comm to find differences
# comm requires sorted input (which we have)
# -23: suppress lines unique to file2 and common lines (show only unique to file1)
# -13: suppress lines unique to file1 and common lines (show only unique to file2)

comm -23 "$TEMP_DIR/coreclr-tests.txt" "$TEMP_DIR/mono-tests.txt" > "$TEMP_DIR/extra-in-coreclr.txt"
comm -13 "$TEMP_DIR/coreclr-tests.txt" "$TEMP_DIR/mono-tests.txt" > "$TEMP_DIR/missing-in-coreclr.txt"

EXTRA_COUNT=$(wc -l < "$TEMP_DIR/extra-in-coreclr.txt")
MISSING_COUNT=$(wc -l < "$TEMP_DIR/missing-in-coreclr.txt")

echo "=========================================="
echo "COMPARISON RESULTS"
echo "=========================================="
echo ""

if [ "$EXTRA_COUNT" -gt 0 ]; then
    echo "### Extra in CoreCLR (not in Mono baseline): $EXTRA_COUNT"
    echo "These tests run on CoreCLR but were skipped on Mono:"
    echo ""
    cat "$TEMP_DIR/extra-in-coreclr.txt" | while read -r line; do
        echo "  - $line"
    done
    echo ""
else
    echo "### Extra in CoreCLR: 0"
    echo "No extra tests in CoreCLR."
    echo ""
fi

if [ "$MISSING_COUNT" -gt 0 ]; then
    echo "### Missing in CoreCLR (in Mono but not CoreCLR): $MISSING_COUNT"
    echo "⚠️  These tests ran on Mono but NOT on CoreCLR (potential issue!):"
    echo ""
    cat "$TEMP_DIR/missing-in-coreclr.txt" | while read -r line; do
        echo "  - $line"
    done
    echo ""
else
    echo "### Missing in CoreCLR: 0"
    echo "✅ All Mono tests also ran on CoreCLR."
    echo ""
fi

# Save comparison results to file
COMPARISON_FILE="${RESULTS_DIR}/test-comparison.txt"
{
    echo "Test Comparison: $TEST_PROJECT_NAME"
    echo "Generated: $(date -Iseconds)"
    echo ""
    echo "Mono tests:    $MONO_COUNT"
    echo "CoreCLR tests: $CORECLR_COUNT"
    echo ""
    echo "=== Extra in CoreCLR ($EXTRA_COUNT) ==="
    cat "$TEMP_DIR/extra-in-coreclr.txt"
    echo ""
    echo "=== Missing in CoreCLR ($MISSING_COUNT) ==="
    cat "$TEMP_DIR/missing-in-coreclr.txt"
} > "$COMPARISON_FILE"

echo "Comparison saved to: $COMPARISON_FILE"
