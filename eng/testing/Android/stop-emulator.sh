#!/usr/bin/env bash

# Shuts down the emulator started by start-emulator.sh, as a Helix post-command.
#
# Best effort by design: the work item result has already been decided by the time this runs, so a
# failure to shut down cleanly must not turn a passing run into a failing one. The container is torn
# down immediately afterwards anyway; this exists so the emulator gets a chance to flush its logs.

set -uo pipefail

android_sdk_root="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-/usr/local/android-sdk}}"
adb_binary="$android_sdk_root/platform-tools/adb"
shutdown_timeout_seconds="${ANDROID_EMULATOR_SHUTDOWN_TIMEOUT:-30}"

if [ ! -x "$adb_binary" ]; then
    echo "adb not found at '$adb_binary'; nothing to stop."
    exit 0
fi

emulator_pid=""
if [ -n "${HELIX_WORKITEM_ROOT:-}" ] && [ -f "$HELIX_WORKITEM_ROOT/.emulator-pid" ]; then
    emulator_pid="$(cat "$HELIX_WORKITEM_ROOT/.emulator-pid")"
fi

# start-emulator.sh only records a pid when it started the emulator itself. On the legacy queues the
# emulator belongs to the machine and is shared by later work items, so it must be left alone.
if [ -z "$emulator_pid" ]; then
    echo "This work item did not start the emulator; leaving it running."
    exit 0
fi

echo "Stopping Android emulator."
"$adb_binary" emu kill > /dev/null 2>&1

if [ -n "$emulator_pid" ]; then
    deadline=$((SECONDS + shutdown_timeout_seconds))
    while [ $SECONDS -lt $deadline ] && kill -0 "$emulator_pid" 2>/dev/null; do
        sleep 1
    done

    if kill -0 "$emulator_pid" 2>/dev/null; then
        echo "Emulator (pid $emulator_pid) did not exit within $shutdown_timeout_seconds seconds; killing it."
        kill -9 "$emulator_pid" 2>/dev/null
    fi
fi

"$adb_binary" kill-server > /dev/null 2>&1

exit 0
