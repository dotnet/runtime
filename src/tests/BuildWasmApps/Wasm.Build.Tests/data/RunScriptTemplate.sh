#!/usr/bin/env bash

# SetCommands defined in eng\testing\tests.wasm.targets
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
	if [[ "$SCENARIO" == "WasmTestOnBrowser" || "$SCENARIO" == "wasmtestonbrowser" ]]; then
		XHARNESS_COMMAND="test-browser"
	else
		XHARNESS_COMMAND="test"
	fi
fi

if [[ "$XHARNESS_COMMAND" == "test" ]]; then
	if [[ -z "$JS_ENGINE" ]]; then
		if [[ "$SCENARIO" == "WasmTestOnNodeJs" || "$SCENARIO" == "wasmtestonnodejs" ]]; then
			JS_ENGINE="--engine=NodeJS"
		else
			JS_ENGINE="--engine=V8"
		fi
	fi

	if [[ -z "$MAIN_JS" ]]; then
		MAIN_JS="--js-file=test-main.js"
	fi

	if [[ -z "$JS_ENGINE_ARGS" ]]; then
		JS_ENGINE_ARGS="--engine-arg=--stack-trace-limit=1000"
	fi
fi

if [[ -z "$XHARNESS_ARGS" ]]; then
	XHARNESS_ARGS="$JS_ENGINE $JS_ENGINE_ARGS $MAIN_JS"
fi

echo EXECUTION_DIR=$EXECUTION_DIR
echo SCENARIO=$SCENARIO
echo XHARNESS_OUT=$XHARNESS_OUT
echo XHARNESS_CLI_PATH=$XHARNESS_CLI_PATH
echo HARNESS_RUNNER=$HARNESS_RUNNER
echo XHARNESS_COMMAND=$XHARNESS_COMMAND
echo MAIN_JS=$MAIN_JS
echo JS_ENGINE=$JS_ENGINE
echo JS_ENGINE_ARGS=$JS_ENGINE_ARGS
echo XHARNESS_ARGS=$XHARNESS_ARGS

function set_env_vars()
{
    if [ "x$TEST_USING_WORKLOADS" = "xtrue" ]; then
        cp -r $BASE_DIR/dotnet-workload $EXECUTION_DIR
        export PATH=$EXECUTION_DIR/dotnet-workload:$PATH
        export SDK_HAS_WORKLOAD_INSTALLED=true
        export SDK_FOR_WORKLOAD_TESTING_PATH=$EXECUTION_DIR/dotnet-workload
        export AppRefDir=$BASE_DIR/microsoft.netcore.app.ref
    else
        cp -r $BASE_DIR/sdk-no-workload $EXECUTION_DIR
        export PATH=$EXECUTION_DIR/sdk-no-workload:$PATH
        export SDK_HAS_WORKLOAD_INSTALLED=false
        export SDK_FOR_WORKLOAD_TESTING_PATH=$EXECUTION_DIR/sdk-no-workload
        export AppRefDir=$BASE_DIR/microsoft.netcore.app.ref
    fi
}

export TEST_LOG_PATH=${XHARNESS_OUT}/logs

pushd $EXECUTION_DIR

# ========================= BEGIN Test Execution ============================= 
echo ----- start $(date) ===============  To repro directly: ===================================================== 
echo pushd $EXECUTION_DIR
# RunCommands defined in eng\testing\tests.wasm.targets
[[RunCommandsEcho]]
echo popd
echo ===========================================================================================================
pushd $EXECUTION_DIR
# RunCommands defined in eng\testing\tests.wasm.targets
[[RunCommands]]
_exitCode=$?
popd
echo ----- end $(date) ----- exit code $_exitCode ----------------------------------------------------------

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
