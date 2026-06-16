#!/bin/bash

set +e

TESTS_DIR="./tests"

if [ -z "$CORE_ROOT" ]; then
    echo "ERROR: CORE_ROOT is not set"
    exit 1
fi

CORERUN="$CORE_ROOT/corerun"

if [ ! -f "$CORERUN" ]; then
    echo "ERROR: corerun not found:"
    echo "$CORERUN"
    exit 1
fi

TMP_PROJ="/tmp/s390x_test_runner"

PASSED_FILE="/tmp/passed_tests.txt"
FAILED_FILE="/tmp/failed_tests.txt"
BUILD_FAILED_FILE="/tmp/build_failed_tests.txt"

rm -f "$PASSED_FILE" "$FAILED_FILE" "$BUILD_FAILED_FILE"
touch "$PASSED_FILE" "$FAILED_FILE" "$BUILD_FAILED_FILE"

echo "Using corerun: $CORERUN"
echo

echo "Creating temporary project..."

rm -rf "$TMP_PROJ"

dotnet new console \
    --framework net9.0 \
    -o "$TMP_PROJ" \
    --force

if [ $? -ne 0 ]; then
    echo "Failed to create temporary project"
    exit 1
fi

PASS=0
FAIL=0
BUILD_FAIL=0
TOTAL=0

for testfile in "$TESTS_DIR"/*.cs
do
    [ -f "$testfile" ] || continue

    TOTAL=$((TOTAL + 1))

    testname=$(basename "$testfile" .cs)

    echo
    echo "============================================================"
    echo "TEST $TOTAL: $testname"
    echo "============================================================"

    echo "Copying source..."
    cp "$testfile" "$TMP_PROJ/Program.cs"

    echo
    echo "BUILDING..."
    echo "------------------------------------------------------------"

    dotnet build "$TMP_PROJ" -c Debug --no-restore

    BUILD_RC=$?

    echo "------------------------------------------------------------"
    echo "Build Exit Code: $BUILD_RC"

    DLL="$TMP_PROJ/bin/Debug/net9.0/s390x_test_runner.dll"

    echo
    echo "EXECUTING:"
    echo "$CORERUN $DLL"
    echo "------------------------------------------------------------"

    "$CORERUN" "$DLL"
    RC=$?

    echo "------------------------------------------------------------"
    echo "Execution Exit Code: $RC"

    if [ $RC -eq 0 ]; then
        echo "[PASS] $testname"

        PASS=$((PASS + 1))
        echo "$testname" >> "$PASSED_FILE"
    else
        echo "[FAIL] $testname"

        FAIL=$((FAIL + 1))
        echo "$testname" >> "$FAILED_FILE"
    fi
done

echo
echo "============================================================"
echo "FINAL SUMMARY"
echo "============================================================"
echo "Total Tests : $TOTAL"
echo "Passed      : $PASS"
echo "Failed      : $FAIL"

echo
echo "============================================================"
echo "PASSED TESTS"
echo "============================================================"

if [ -s "$PASSED_FILE" ]; then
    cat "$PASSED_FILE"
else
    echo "None"
fi

echo
echo "============================================================"
echo "FAILED TESTS"
echo "============================================================"

if [ -s "$FAILED_FILE" ]; then
    cat "$FAILED_FILE"
else
    echo "None"
fi

