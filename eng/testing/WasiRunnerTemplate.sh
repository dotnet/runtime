#!/usr/bin/env bash

# SetCommands defined in eng\testing\tests.wasi.targets
[[SetCommands]]
[[SetCommandsEcho]]

EXECUTION_DIR=$(dirname $0)
if [[ -n "$3" ]]; then
	SCENARIO=$3
fi

if [[ -z "$HELIX_WORKITEM_UPLOAD_ROOT" ]]; then
	XHARNESS_OUT="$EXECUTION_DIR/xharness-output"
else
	XHARNESS_OUT="$HELIX_WORKITEM_UPLOAD_ROOT/xharness-output"
fi

if [[ -n "$XHARNESS_CLI_PATH" ]]; then
	# When running in CI, we only have the .NET runtime available
	# We need to call the XHarness CLI DLL directly via dotnet exec
	HARNESS_RUNNER="dotnet exec $XHARNESS_CLI_PATH"
else
	HARNESS_RUNNER="dotnet xharness"
fi

if [[ -z "$XHARNESS_COMMAND" ]]; then
    XHARNESS_COMMAND="test"
fi

if [[ -z "$XHARNESS_ARGS" ]]; then
	XHARNESS_ARGS="$ENGINE_ARGS"
fi

if [[ -n "$PREPEND_PATH" ]]; then
    export PATH=$PREPEND_PATH:$PATH
fi

if [[ -n "$XUNIT_RANDOM_ORDER_SEED" ]]; then
    WasmXHarnessMonoArgs="${WasmXHarnessMonoArgs} --env=XUNIT_RANDOM_ORDER_SEED=${XUNIT_RANDOM_ORDER_SEED}"
fi

echo EXECUTION_DIR=$EXECUTION_DIR
echo SCENARIO=$SCENARIO
echo XHARNESS_OUT=$XHARNESS_OUT
echo XHARNESS_CLI_PATH=$XHARNESS_CLI_PATH
echo HARNESS_RUNNER=$HARNESS_RUNNER
echo XHARNESS_COMMAND=$XHARNESS_COMMAND
echo XHARNESS_ARGS=$XHARNESS_ARGS

pushd $EXECUTION_DIR

# ========================= BEGIN Test Execution ============================= 
echo ----- start $(date) ===============  To repro directly: ===================================================== 
echo pushd $EXECUTION_DIR
# RunCommands defined in eng\testing\tests.wasi.targets
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd $EXECUTION_DIR
# RunCommands defined in eng\testing\tests.wasi.targets
[[RunCommands]]
_exitCode=$?
popd
echo ----- end $(date) ----- exit code $_exitCode ----------------------------------------------------------

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
