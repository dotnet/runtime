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

xharness android test -i="net.dot.MonoRunner" \
    --package-name="net.dot.$TEST_NAME" \
    --app=$APK -o=$EXECUTION_DIR/TestResults -v
