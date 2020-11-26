#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)

cd $EXECUTION_DIR

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

XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

if [ ! -z "$XHARNESS_CLI_PATH" ]; then
	# When running in CI, we only have the .NET runtime available
	# We need to call the XHarness CLI DLL directly via dotnet exec
	HARNESS_RUNNER="dotnet exec $XHARNESS_CLI_PATH"
else
	HARNESS_RUNNER="dotnet xharness"
fi

# RunCommands defined in tests.mobile.targets
[[RunCommands]]

_exitCode=$?

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
