#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)
SCENARIO=$3

cd $EXECUTION_DIR

if [ -z "$HELIX_WORKITEM_UPLOAD_ROOT" ]; then
	XHARNESS_OUT="$EXECUTION_DIR/xharness-output"
else
	XHARNESS_OUT="$HELIX_WORKITEM_UPLOAD_ROOT/xharness-output"
fi

if [ ! -z "$XHARNESS_CLI_PATH" ]; then
	# When running in CI, we only have the .NET runtime available
	# We need to call the XHarness CLI DLL directly via dotnet exec
	HARNESS_RUNNER="dotnet exec $XHARNESS_CLI_PATH"
else
	HARNESS_RUNNER="dotnet xharness"
fi

if [ "$SCENARIO" == "WasmTestOnBrowser" ]; then
	XHARNESS_COMMAND="test-browser"
elif [ -z "$XHARNESS_COMMAND" ]; then
	XHARNESS_COMMAND="test"
fi

# RunCommands defined in tests.mobile.targets
[[RunCommands]]

_exitCode=$?

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
