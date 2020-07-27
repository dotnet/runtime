#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
[[RunCommands]]

if [ "$TARGET_ARCH" == "arm" ]; then
    TARGET=ios-device
    SCHEME_SDK=Release-iphoneos
elif [ "$TARGET_ARCH" == "arm64" ]; then
    TARGET=ios-device
    SCHEME_SDK=Release-iphoneos
elif [ "$TARGET_ARCH" == "x64" ]; then
    TARGET=ios-simulator-64
    SCHEME_SDK=Release-iphonesimulator
elif [ "$TARGET_ARCH" == "x86" ]; then
    TARGET=ios-simulator-32
    SCHEME_SDK=Release-iphonesimulator
else
    echo "Unknown architecture: $TARGET_ARCH"
    exit 1
fi

# "Release" in SCHEME_SDK is what xcode produces (see "bool Optimized" property in AppleAppBuilderTask)

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

# Restart the simulator to make sure it is tied to the right user session
XCODE_VERSION=11.4
XCODE_PATH="/Applications/Xcode${XCODE_VERSION/./}.app"
SIMULATOR_APP="$XCODE_PATH/Contents/Developer/Applications/Simulator.app"
sudo pkill -9 -f "$SIMULATOR_APP"
open -a "$SIMULATOR_APP"
export XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

dotnet xharness ios test  \
    --targets="$TARGET"   \
    --app="$APP_BUNDLE"   \
    --xcode="$XCODE_PATH" \
    --output-directory=$XHARNESS_OUT

_exitCode=$?

echo "XHarness artifacts: `ls -lh $XHARNESS_OUT`"

# Kill the simulator after we're done
sudo pkill -9 -f "$SIMULATOR_APP"

exit $_exitCode
