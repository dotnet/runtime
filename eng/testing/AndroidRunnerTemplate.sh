#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
TEST_NAME=$1
TARGET_ARCH=$2

APK=$EXECUTION_DIR/Bundle/bin/$TEST_NAME.apk

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


## XHarness doesn't support macOS/Linux yet (in progress) so we'll use a hand-made adb script
# dotnet xharness android test -i="net.dot.MonoRunner" \
# --package-name="net.dot.$TEST_NAME" \
# --app=$APK -o=$EXECUTION_DIR/Bundle/TestResults -v

ADB=$ANDROID_SDK_ROOT/platform-tools/adb
echo "Installing net.dot.$TEST_NAME on an active device/emulator..."
$ADB uninstall net.dot.$TEST_NAME > /dev/null 2>&1 || true
$ADB install "$APK"
echo "Running tests for $TEST_NAME (see live logs via logcat)..."
$ADB shell am instrument -w net.dot.$TEST_NAME/net.dot.MonoRunner
echo "Finished. See logcat for details, e.g. '$ADB logcat DOTNET:V -s'"
