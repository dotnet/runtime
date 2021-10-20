#!/usr/bin/env bash

EXECUTION_DIR=$(dirname $0)

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

function set_env_vars()
{
    if [ "x$TEST_USING_WORKLOADS" = "xtrue" ]; then
        export PATH=$BASE_DIR/dotnet-workload:$PATH
        export SDK_HAS_WORKLOAD_INSTALLED=true
        export SDK_FOR_WORKLOAD_TESTING_PATH=$BASE_DIR/dotnet-workload
        export AppRefDir=$BASE_DIR/microsoft.netcore.app.ref
    elif [ ! -z "$HELIX_WORKITEM_UPLOAD_ROOT" ]; then
        export WasmBuildSupportDir=$BASE_DIR/build
    else
        export PATH=$BASE_DIR/sdk-no-workload:$PATH
        export SDK_HAS_WORKLOAD_INSTALLED=false
        export SDK_FOR_WORKLOAD_TESTING_PATH=$BASE_DIR/sdk-no-workload
    fi
}

export TEST_LOG_PATH=${XHARNESS_OUT}/logs

[[RunCommands]]

_exitCode=$?

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
