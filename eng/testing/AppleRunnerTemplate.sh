#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
[[RunCommands]]

TARGET_DEVICE=ios-device
TARGET_SIMULATOR=ios-simulator-64
TARGET_SDK=iphoneos
TARGET_SIMULATOR_SDK=iphonesimulator

if [ "$TARGET_OS" == "tvOS" ]; then
    TARGET_DEVICE=tvos-device
    TARGET_SIMULATOR=tvos-simulator
    TARGET_SDK=appletvos
    TARGET_SIMULATOR_SDK=appletvsimulator
fi


# "Release" in SCHEME_SDK is what xcode produces (see "bool Optimized" property in AppleAppBuilderTask)
if [ "$TARGET_ARCH" == "arm" ]; then
    TARGET=$TARGET_DEVICE
    SCHEME_SDK="Release-$TARGET_SDK"
elif [ "$TARGET_ARCH" == "arm64" ]; then
    TARGET=$TARGET_DEVICE
    SCHEME_SDK="Release-$TARGET_SDK"
elif [ "$TARGET_ARCH" == "x64" ]; then
    TARGET=$TARGET_SIMULATOR
    SCHEME_SDK="Release-$TARGET_SIMULATOR_SDK"
elif [ "$TARGET_ARCH" == "x86" ] && [ "$TARGET_OS" == "iOS" ]; then
    TARGET=ios-simulator-32
    SCHEME_SDK="Release-$TARGET_SIMULATOR_SDK"
else
    echo "Unknown OS & architecture: $TARGET_OS $TARGET_ARCH"
    exit 1
fi

APP_BUNDLE=$EXECUTION_DIR/$TEST_NAME/$SCHEME_SDK/$TEST_NAME.app

# it doesn't support parallel execution yet, so, here is a hand-made semaphore:
LOCKDIR=/tmp/runonsim.lock
while true; do
    if mkdir "$LOCKDIR"
    then
        trap 'rm -rf "$LOCKDIR"' 0
        break
    else
        sleep 5
    fi
done

XCODE_PATH="`xcode-select -p`/../.."

if [ -z "$HELIX_WORKITEM_UPLOAD_ROOT" ]; then
    export XHARNESS_OUT="$EXECUTION_DIR/xharness-output"
else
    export XHARNESS_OUT="$HELIX_WORKITEM_UPLOAD_ROOT/xharness-output"
fi

dotnet xharness ios test  \
    --targets="$TARGET"   \
    --app="$APP_BUNDLE"   \
    --xcode="$XCODE_PATH" \
    --output-directory=$XHARNESS_OUT

_exitCode=$?

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
