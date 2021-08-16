#!/usr/bin/env bash

# NOTE: this script is only used locally, on CI we use the Helix SDK from arcade

EXECUTION_DIR=$(dirname $0)
ASSEMBLY_NAME=$1
TARGET_ARCH=$2
TARGET_OS=$3
TEST_NAME=$4
XHARNESS_CMD="test"
XHARNESS_OUT="$EXECUTION_DIR/xharness-output"
XCODE_PATH=$(xcode-select -p)/../..

if [ -n "$5" ]; then
    XHARNESS_CMD="run"
    ADDITIONAL_ARGS=${@:5}
fi

if [[ "$TARGET_OS" == "maccatalyst" ]]; then TARGET=maccatalyst; fi

if [[ "$TARGET_OS" == "iossimulator" && "$TARGET_ARCH" == "x86" ]]; then TARGET=ios-simulator-32; fi
if [[ "$TARGET_OS" == "iossimulator" && "$TARGET_ARCH" == "x64" ]]; then TARGET=ios-simulator-64; fi
if [[ "$TARGET_OS" == "iossimulator" && "$TARGET_ARCH" == "arm64" ]]; then TARGET=ios-simulator-64; fi
if [[ "$TARGET_OS" == "ios" && "$TARGET_ARCH" == "arm" ]]; then TARGET=ios-device; fi
if [[ "$TARGET_OS" == "ios" && "$TARGET_ARCH" == "arm64" ]]; then TARGET=ios-device; fi

if [[ "$TARGET_OS" == "tvossimulator" && "$TARGET_ARCH" == "x64" ]]; then TARGET=tvos-simulator; fi
if [[ "$TARGET_OS" == "tvossimulator" && "$TARGET_ARCH" == "arm64" ]]; then TARGET=tvos-simulator; fi
if [[ "$TARGET_OS" == "tvos" && "$TARGET_ARCH" == "arm64" ]]; then TARGET=tvos-device; fi

# "Release" in SCHEME_SDK is what xcode produces (see "bool Optimized" property in AppleAppBuilderTask)
if [[ "$TARGET" == "ios-simulator-"* ]]; then SCHEME_SDK=Release-iphonesimulator; fi
if [[ "$TARGET" == "tvos-simulator" ]]; then SCHEME_SDK=Release-appletvsimulator; fi
if [[ "$TARGET" == "ios-device" ]]; then SCHEME_SDK=Release-iphoneos; fi
if [[ "$TARGET" == "tvos-device" ]]; then SCHEME_SDK=Release-appletvos; fi
if [[ "$TARGET" == "maccatalyst" ]]; then SCHEME_SDK=Release-maccatalyst; fi

cd $EXECUTION_DIR

# it doesn't support parallel execution yet, so, here is a hand-made semaphore:
LOCKDIR=/tmp/appletests.lock
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

$HARNESS_RUNNER apple $XHARNESS_CMD    \
    --app="$EXECUTION_DIR/$TEST_NAME/$SCHEME_SDK/$TEST_NAME.app" \
    --targets="$TARGET" \
    --xcode="$XCODE_PATH"   \
    --output-directory="$XHARNESS_OUT" \
    $ADDITIONAL_ARGS

_exitCode=$?

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
