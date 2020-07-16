#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
TEST_NAME=$1
TARGET_ARCH=$2
TARGET=
SCHEME_SDK=

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

XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

dotnet xharness ios test --app="$APP_BUNDLE" \
    --targets=$TARGET \
    --output-directory=$XHARNESS_OUT

_exitCode=$?

echo "Xharness artifacts: $XHARNESS_OUT"

exit $_exitCode
