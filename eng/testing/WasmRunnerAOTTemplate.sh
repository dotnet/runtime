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

function _buildAOTFunc()
{
	local projectFile=$1
	local binLog=$2
	shift 2

	time dotnet msbuild $projectFile /bl:$binLog $*
	local buildExitCode=$?

	echo "\n** Performance summary for the build **\n"
	dotnet msbuild $binLog -clp:PerformanceSummary -v:q -nologo
	if [[ "$(uname -s)" == "Linux" && $buildExitCode -ne 0 ]]; then
		echo "\nLast few messages from dmesg:\n"
		local lastLines=`dmesg | tail -n 20`
		echo $lastLines

		if [[ "$lastLines" =~ "oom-kill" ]]; then
			return 9200 # OOM
		fi
	fi

	echo
	echo

    if [[ $buildExitCode -ne 0 ]]; then
        return 9100 # aot build failure
    fi

	return 0
}

# RunCommands defined in tests.mobile.targets
[[RunCommands]]

_exitCode=$?

echo "XHarness artifacts: $XHARNESS_OUT"

exit $_exitCode
