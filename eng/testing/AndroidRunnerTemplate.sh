#!/usr/bin/env bash

# NOTE: this script is only used locally, on CI we use the Helix SDK from arcade

EXECUTION_DIR=$(dirname $0)
ASSEMBLY_NAME=$1
TARGET_ARCH=$2
TARGET_OS=$3
TEST_NAME=$4
XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

if [ -n "$5" ]; then
    ADDITIONAL_ARGS=${@:5}
fi

cd $EXECUTION_DIR

# it doesn't support parallel execution yet, so, here is a hand-made semaphore:
LOCKDIR=/tmp/androidtests.lock
while true; do
    if mkdir "$LOCKDIR"
    then
        trap 'rm -rf "$LOCKDIR"' 0
        break
    else
        sleep 5
    fi
done

if [ ! -z "$XHARNESS_CLI_PATH" ]; then
    # Allow overriding the path to the XHarness CLI DLL,
    # we need to call it directly via dotnet exec
    HARNESS_RUNNER="dotnet exec $XHARNESS_CLI_PATH"
else
	HARNESS_RUNNER="dotnet xharness"
fi

$HARNESS_RUNNER android test                \
    --instrumentation="net.dot.MonoRunner"  \
    --package-name="net.dot.$ASSEMBLY_NAME" \
    --app="$EXECUTION_DIR/bin/$TEST_NAME.apk" \
    --output-directory="$XHARNESS_OUT" \
    --timeout=1800 \
    $ADDITIONAL_ARGS

_exitCode=$?

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
