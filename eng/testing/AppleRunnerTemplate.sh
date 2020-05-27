#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
[[RunCommands]]

#if [ "$TARGET_ARCH" == "arm" ]; then
#    TARGET=ios-device
#    SCHEME_SDK=Release-iphoneos
#elif [ "$TARGET_ARCH" == "arm64" ]; then
#    TARGET=ios-device
#    SCHEME_SDK=Release-iphoneos
#elif [ "$TARGET_ARCH" == "x64" ]; then
#    TARGET=ios-simulator-64
#    SCHEME_SDK=Release-iphonesimulator
#elif [ "$TARGET_ARCH" == "x86" ]; then
#    TARGET=ios-simulator-32
#    SCHEME_SDK=Release-iphonesimulator
#else
#    echo "Unknown architecture: $TARGET_ARCH"
#    exit 1
#fi

# "Release" in SCHEME_SDK is what xcode produces (see "bool Optimized" property in AppleAppBuilderTask)

#APP_BUNDLE=$EXECUTION_DIR/$TEST_NAME/$SCHEME_SDK/$TEST_NAME.app
APK=$EXECUTION_DIR/$TEST_NAME.app

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

if [ -x "$(command -v xharness)" ]
then
    dotnet xharness ios test -i="net.dot.MonoRunner" \
        --package-name="net.dot.$TEST_NAME" \
        --app="$APP_BUNDLE" 
        -o=$HELIX_WORKITEM_UPLOAD_ROOT -v

    cp $HELIX_WORKITEM_UPLOAD_ROOT/*.xml ./
else
    dotnet xharness ios test -i="net.dot.MonoRunner" \
        --package-name="net.dot.$TEST_NAME" \
        --app="$APP_BUNDLE" 
        -o=$EXECUTION_DIR/TestResults -v
fi

_exitCode=$?

echo "Xharness artifacts: $XHARNESS_OUT"

exit $_exitCode
