#!/usr/bin/env bash

# create dummy console app to workaround https://github.com/dotnet/runtime/issues/80619
(CONSOLE_TEMP_DIR="$(mktemp -d)"; "$DOTNET_ROOT/dotnet" new console -o "$CONSOLE_TEMP_DIR"; rm -rf "$CONSOLE_TEMP_DIR") || true

set
echo "------------------------ start -------------------"

[[SetCommands]]
[[SetCommandsEcho]]

EXECUTION_DIR=$(dirname $0)

if [[ -z "$HELIX_WORKITEM_UPLOAD_ROOT" ]]; then
	XHARNESS_OUT="$EXECUTION_DIR/xharness-output"
else
	XHARNESS_OUT="$HELIX_WORKITEM_UPLOAD_ROOT/xharness-output"
fi

if [[ -n "$PREPEND_PATH" ]]; then
    export PATH=$PREPEND_PATH:$PATH
fi

echo EXECUTION_DIR=$EXECUTION_DIR
echo XHARNESS_OUT=$XHARNESS_OUT
echo XHARNESS_CLI_PATH=$XHARNESS_CLI_PATH

function set_env_vars()
{
    if [ "x$TEST_USING_WORKLOADS" = "xtrue" ]; then
        export SDK_HAS_WORKLOAD_INSTALLED=true
    else
        export SDK_HAS_WORKLOAD_INSTALLED=false
    fi

    if [ "x$TEST_USING_WEBCIL" = "xfalse" ]; then
        export USE_WEBCIL_FOR_TESTS=false
    else
        export USE_WEBCIL_FOR_TESTS=true
    fi

    local _SDK_DIR=
    if [[ -n "$HELIX_WORKITEM_UPLOAD_ROOT" ]]; then
        cp -r $BASE_DIR/$SDK_DIR_NAME $EXECUTION_DIR
        _SDK_DIR=$EXECUTION_DIR/$SDK_DIR_NAME
    else
        _SDK_DIR=$BASE_DIR/$SDK_DIR_NAME
    fi

    export PATH=$_SDK_DIR:$PATH
    export SDK_FOR_WORKLOAD_TESTING_PATH=$_SDK_DIR
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
