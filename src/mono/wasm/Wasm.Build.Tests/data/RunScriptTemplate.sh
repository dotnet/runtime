#!/usr/bin/env bash

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

export SHOW_BUILD_OUTPUT=1

echo EXECUTION_DIR=$EXECUTION_DIR
echo XHARNESS_OUT=$XHARNESS_OUT
echo XHARNESS_CLI_PATH=$XHARNESS_CLI_PATH
echo SHOW_BUILD_OUTPUT=$SHOW_BUILD_OUTPUT

echo "User:" && whoami
echo "Session ID:" && cat /proc/self/sessionid && echo ""
echo "Info about /"
ls -lad /
echo "Info about /tmp/"
ls -lad /tmp/
echo "Info about /tmp/.dotnet/"
ls -lad /tmp/.dotnet/
echo "Contents of /tmp/.dotnet/"
ls -la /tmp/.dotnet/
echo "Contents of /tmp/.dotnet/shm/"
ls -la /tmp/.dotnet/shm/

function set_env_vars()
{
    if [ "x$TEST_USING_WORKLOADS" = "xtrue" ]; then
        export SDK_HAS_WORKLOAD_INSTALLED=true
    else
        export SDK_HAS_WORKLOAD_INSTALLED=false
    fi

    if [ "x$TEST_USING_WEBCIL" = "xtrue" ]; then
        export USE_WEBCIL_FOR_TESTS=true
    else
        export USE_WEBCIL_FOR_TESTS=false
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
