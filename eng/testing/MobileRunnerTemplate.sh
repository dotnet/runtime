#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
TEST_NAME=$1
TARGET_ARCH=$2
TARGET=
SCHEME_SDK=

if [ "$TARGET_ARCH" == "arm64" ]; then
    TARGET=ios-device
    SCHEME_SDK=Release-iphoneos
else
    TARGET=ios-simulator-64
    SCHEME_SDK=Release-iphonesimulator
fi

# Release here is what xcode produces (see "bool Optimized" property in AppleAppBuilderTask)
APP_BUNDLE=$EXECUTION_DIR/Bundle/$TEST_NAME/$SCHEME_SDK/$TEST_NAME.app

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

dotnet xharness ios test --app="$APP_BUNDLE" \
    --targets=$TARGET \
    --output-directory="$EXECUTION_DIR/Bundle/xharness-output" \
    --working-directory="$EXECUTION_DIR/Bundle/xharness-workingdir"
