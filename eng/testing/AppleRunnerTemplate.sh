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

APP_BUNDLE=$EXECUTION_DIR/$TEST_NAME.app

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

export XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

if [ -x "$(command -v xharness)" ]
then
    echo 'Xharness command is in $PATH'
    export XHARNESS_DISABLE_COLORED_OUTPUT=true
    export XHARNESS_LOG_WITH_TIMESTAMPS=true
    xharness ios test \
        --targets="$TARGET" \
        --app="$APP_BUNDLE" \
        --xcode="/Applications/Xcode114.app" \
        --output-directory=$XHARNESS_OUT
    echo "Output files:"
else
    echo 'Xharness command is NOT in $PATH'
    dotnet xharness ios test \
        --targets="$TARGET" \
        --app="$APP_BUNDLE" \
        --xcode="/Applications/Xcode114.app" \
        --output-directory=$XHARNESS_OUT
fi

_exitCode=$?

echo "Xharness artifacts: `ls -lh $XHARNESS_OUT`"

exit $_exitCode
