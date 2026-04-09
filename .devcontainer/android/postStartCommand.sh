#!/usr/bin/env bash

set -e

# Start the emulator.
# Use nohup to start a shell so that things run in the background.
# Use flock to make sure we have only one running emulator.
# Background flock
# nohup stdout and stderr goes to /dev/null, otherwise it's going to write to a file.
nohup bash -c 'flock -n /var/lock/emulator.lock -c '\''${ANDROID_SDK_ROOT}/emulator/emulator -avd ${EMULATOR_NAME_X64} -no-window -no-audio -no-snapstorage'\''&' >/dev/null 2>&1
