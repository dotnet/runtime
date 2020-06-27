#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)

cd $EXECUTION_DIR

XHARNESS_OUT="$EXECUTION_DIR/xharness-output"

if [ ! -x "$(command -v xharness)" ]; then
	HARNESS_RUNNER="dotnet"
fi

# RunCommands defined in tests.mobile.targets
[[RunCommands]]

_exitCode=$?

echo "Xharness artifacts: $XHARNESS_OUT"

exit $_exitCode
