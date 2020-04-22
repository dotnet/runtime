#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
TEST_NAME=$1
SIMULATOR_NAME="iPhone 11"

# TODO: to be replaced with xharness cli tool

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

# Release here is what xcode produces (see "bool Optimized" property in AppleAppBuilderTask)
AppBundlePath=$EXECUTION_DIR/Bundle/$TEST_NAME/Release-iphonesimulator/$TEST_NAME.app

# kill a simulator if it exists
xcrun simctl shutdown "$SIMULATOR_NAME" || true

# boot it again
xcrun simctl boot "$SIMULATOR_NAME"

# open UI (this step is not neccessary)
open -a Simulator

# install the *.app bundle
xcrun simctl install "$SIMULATOR_NAME" "$SIMULATOR_NAME"

# launch the app, redirect logs to console and quite once tests are completed
xcrun simctl launch --console booted net.dot.$TEST_NAME testlib:$TEST_NAME.dll --auto-exit
