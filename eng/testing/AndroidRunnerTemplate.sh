#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
TEST_NAME=$1
TARGET_ARCH=$2

APK=$EXECUTION_DIR/bin/$TEST_NAME.apk

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

XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

dotnet xharness android test --instrumentation="net.dot.MonoRunner" \
    --package-name="net.dot.$TEST_NAME" \
    --app=$APK --output-directory=$XHARNESS_OUT -v

_exitCode=$?

echo "Xharness artifacts: $XHARNESS_OUT"

exit $_exitCode
