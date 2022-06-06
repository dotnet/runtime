#!/usr/bin/env bash

. TestEnv.txt

EXECUTION_DIR="$(realpath "$(dirname "$0")")"
RUNTIME_PATH="$2"
TEST_SCRIPT="$(basename "$ASSEMBLY_NAME" .dll).sh"
if [[ -z "$HELIX_WORKITEM_UPLOAD_ROOT" ]]; then
        XHARNESS_OUT="$EXECUTION_DIR/xharness-output"
else
        XHARNESS_OUT="$HELIX_WORKITEM_UPLOAD_ROOT/xharness-output"
fi

if [[ -n "$3" ]]; then
    ADDITIONAL_ARGS=${*:5}
fi

if [[ "x$1" != "x--runtime-path" ]]; then
    echo "You must specify the runtime path with --runtime-path /path/to/runtime"
    exit 1
fi

RUNTIME_PATH="$(realpath "$RUNTIME_PATH")"

cd "$EXECUTION_DIR" || exit 1

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

if [[ -n "$XHARNESS_CLI_PATH" ]]; then
    # Allow overriding the path to the XHarness CLI DLL,
    # we need to call it directly via dotnet exec
    HARNESS_RUNNER="dotnet exec $XHARNESS_CLI_PATH"
else
        HARNESS_RUNNER="dotnet xharness"
fi

$HARNESS_RUNNER android-headless test                \
    --test-path="$EXECUTION_DIR" \
    --runtime-folder="$RUNTIME_PATH" \
    --test-assembly="$ASSEMBLY_NAME" \
    --device-arch="$TEST_ARCH" \
    --test-script="$TEST_SCRIPT" \
    --output-directory="$XHARNESS_OUT" \
    --timeout=1800 -v \
    $ADDITIONAL_ARGS

_exitCode=$?

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
