#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
[[RunCommands]]

APK=$EXECUTION_DIR/$TEST_NAME.apk

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

if [ -x "$(command -v xharness)" ]
then
    xharness android test -i="net.dot.MonoRunner" \
        --package-name="net.dot.$TEST_NAME" \
        --app=$APK -o=$HELIX_WORKITEM_UPLOAD_ROOT -v
    
    cp $HELIX_WORKITEM_UPLOAD_ROOT/*.xml ./
else
    dotnet xharness android test -i="net.dot.MonoRunner" \
        --package-name="net.dot.$TEST_NAME" \
        --app=$APK -o=$EXECUTION_DIR/TestResults -v
fi
