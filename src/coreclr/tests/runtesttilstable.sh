#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'CoreCLR test runner wrapper script.'
    echo ''
    echo 'Run tests using runtest.sh, then rerun the failures, if any,'
    echo 'until the number of failures stabilizes. Thus, when running'
    echo 'flaky tests, or running tests on a flaky platform, only the'
    echo 'repeatable, "real", failures are reported.'
    echo ''
    echo 'Tests are rerun in sequential mode (passing --sequential to runtest.sh).'
    echo 'This hopefully avoids resource exhaustion and other parallel run problems.'
    echo ''
    echo 'A maximum number of iterations can be specified.'
    echo ''
    echo 'Command line:'
    echo ''
    echo 'runtesttilstable.sh [options] [arguments for runtest.sh]'
    echo ''
    echo 'Any unknown argument is passed directly to runtest.sh.'
    echo ''
    echo 'Optional arguments:'
    echo '  -h|--help                        : Show usage information.'
    echo '  --max-iterations=<count>         : Specify the maximum number of iterations. Default: 4.'
    echo ''
}

function exit_with_error {
    local errorMessage=$1
    local printUsage=$2

    if [ -z "$printUsage" ]; then
        ((printUsage = 0))
    fi

    echo "$errorMessage"
    if ((printUsage != 0)); then
        print_usage
    fi
    exit $EXIT_CODE_EXCEPTION
}

# Handle Ctrl-C. We will stop execution and print the results that
# we gathered so far.
function handle_ctrl_c {
    echo ""
    echo "*** Stopping... ***"
    print_results
    exit_with_error "Test run aborted by Ctrl+C."
}

# Register the Ctrl-C handler
trap handle_ctrl_c INT

# Where are we?
scriptPath=$(dirname $0)

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.
readonly EXIT_CODE_EXCEPTION=1     # Script exited because something exceptional happened (e.g. bad arguments, Ctrl-C interrupt).
readonly EXIT_CODE_TEST_FAILURE=2  # Script completed successfully, but one or more tests failed.

# Argument variables
((maxIterations = 20))

# Handle arguments
__UnprocessedBuildArgs=

# We need to capture the --testRootDir argument so we know where the test pass/fail/skip files will be placed.
testRootDir=

# We need to handle the --playlist argument specially. The first run, we pass it through (if passed).
# After that, we use the --playlist argument ourselves, so we don't pass through the original one.
playlistArgument=

for i in "$@"
do
    case $i in
        -h|--help)
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
        --max-iterations=*)
            maxIterations=${i#*=}
            ;;
        --playlist=*)
            playlistArgument=$i
            ;;
        --testRootDir=*)
            testRootDir=${i#*=}
            # Also pass it on to runtest.sh
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
            ;;
    esac
done

# Check testRootDir; this check is also done by runtest.sh.

if [ -z "$testRootDir" ]; then
    echo "--testRootDir is required."
    print_usage
    exit $EXIT_CODE_EXCEPTION
fi
if [ ! -d "$testRootDir" ]; then
    echo "Directory specified by --testRootDir does not exist: $testRootDir"
    exit $EXIT_CODE_EXCEPTION
fi

# Now start running the tests.

nextcmd="${scriptPath}/runtest.sh ${playlistArgument} ${__UnprocessedBuildArgs}"
echo "Running: $nextcmd"
$nextcmd
exitCode=$?
if [ $exitCode -eq $EXIT_CODE_TEST_FAILURE ]; then
    # Now, we loop, rerunning the failed tests up to maxIterations times minus one
    # (the initial run counts as an iteration).
    ((totalRerunCount = $maxIterations - 1))
    for (( i=1; i<=$totalRerunCount; i++ )); do
        if [ ! -e "$testRootDir/coreclrtests.fail.txt" ]; then
            exit_with_error "Error: couldn't find $testRootDir/coreclrtests.fail.txt"
        fi

        num_errors=$(grep -c '' "$testRootDir/coreclrtests.fail.txt")
        echo "Test run failed with $num_errors errors:"
        cat "$testRootDir/coreclrtests.fail.txt"
        echo ''

        echo "Rerunning failures ($i of $totalRerunCount reruns)..."

        # Move the fail file to a different location, so it can be used without getting trashed by the
        # next run's error file.
        retryFile="$testRootDir/coreclrtests.retry.txt"
        if [ -e "$retryFile" ]; then
            rm -f "$retryFile"
            if [ -e "$retryFile" ]; then
                exit_with_error "Error: couldn't delete $retryFile"
            fi
        fi
        mv "$testRootDir/coreclrtests.fail.txt" "$retryFile"

        nextcmd="${scriptPath}/runtest.sh --sequential --playlist=${retryFile} ${__UnprocessedBuildArgs}"
        echo "Running: $nextcmd"
        $nextcmd
        exitCode=$?
        if [ $exitCode -ne $EXIT_CODE_TEST_FAILURE ]; then
            # Either success or exceptional failure; we're done. For test failure, we loop,
            # if we haven't hit the maximum number of allowed iterations.
            break
        fi
    done
fi

exit $exitCode
