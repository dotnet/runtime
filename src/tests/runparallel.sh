#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'CoreCLR parallel test runner script.'
    echo ''
    echo 'Required arguments:'
    echo '  --testRootDir=<path>             : Root directory of the test build (e.g. coreclr/artifacts/tests/windows.x64.Debug).'
    echo '  --coreOverlayDir=<path>          : Directory containing core binaries and test dependencies. If not specified, the'
    echo '                                     default is testRootDir/Tests/coreoverlay. This switch overrides --coreClrBinDir,'
    echo '                                     --mscorlibDir, --coreFxBinDir, and --coreFxNativeBinDir.'
    echo '  --playlist=<path>                : Run only the tests that are specified in the file at <path>, in the same format as'
    echo ''
    echo 'Optional arguments:'
    echo '  -h|--help                        : Show usage information.'
    echo '  --test-env                       : Script to set environment variables for tests'
    echo ''
}

function print_results {
    echo -e \
       "Results @ $(date +%r)\n\t"\
       "Pass:$countPassedTests\t"\
       "Fail:$countFailedTests\n\t"\
       "Finish:$countFinishedTests\t"\
       "Incomplete: $countIncompleteTests\n\t"\
       "Total: $countTotalTests"
}

function update_results {
    # Initialize counters for bookkeeping.
    countTotalTests=$(wc -l < $playlistFile)

    countPassedTests=$(cd results.int; grep -r 'RUN_TEST_EXIT_CODE=0' . | wc -l)
    countFailedTests=$(cd results.int; grep -r 'RUN_TEST_EXIT_CODE=[^0]' . | wc -l)
    countFinishedTests=$((countPassedTests + countFailedTests))
    countIncompleteTests=$((countTotalTests - countFinishedTests))

    print_results
}

function print_elapsed_time {
    time_end=$(date +"%s")
    time_diff=$(($time_end-$time_start))
    echo "$(($time_diff / 60)) minutes and $(($time_diff % 60)) seconds taken to run CoreCLR tests."
}



# Handle Ctrl-C. We will stop execution and print the results that
# we gathered so far.
function handle_ctrl_c {
    local errorSource='handle_ctrl_c'

    echo ""
    echo "*** Stopping... ***"

    update_results
    echo "$errorSource" "Test run aborted by Ctrl+C."

    print_elapsed_time

    exit 1
}

function cleanup
{
    kill -TERM % >/dev/null
    mv results.int results.$(date +%F_%I%M%p)
}


prep_test() {
    # This function runs in a background process. It should not echo anything, and should not use global variables.

    #remove any NI
    rm -f $(dirname "$1")/*.ni.*
}

run_test() {
    # This function runs in a background process. It should not echo anything, and should not use global variables.

    local scriptFilePath=$1

    local scriptFileName=$(basename "$scriptFilePath")
    local outputFileName=$PWD/results.int/${scriptFilePath////.}

    nice "$scriptFilePath" -debug=$(which time) >"$outputFileName" 2>&1

    echo RUN_TEST_EXIT_CODE="$?" >>"$outputFileName"
}

# Argument variables
testRootDir=
coreOverlayDir=
testEnv=
playlistFile=

for i in "$@"
do
    case $i in
        -h|--help)
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
        --coreOverlayDir=*)# Exit code constants
            coreOverlayDir=${i#*=}
            ;;
        --playlist=*)
            playlistFile=${i#*=}
            ;;
        --testRootDir=*)
            testRootDir=${i#*=}
            ;;
        --test-env=*)
            testEnv=${i#*=}
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

if [ -z "$coreOverlayDir" ]; then
    echo "--coreOverlayDir is required."
    print_usage
    exit $EXIT_CODE_EXCEPTION
fi

if [ ! -d "$coreOverlayDir" ]; then
    echo "Directory specified by --coreOverlayDir does not exist: $coreOverlayDir"
    exit $EXIT_CODE_EXCEPTION
fi

if [ -z "$playlistFile" ]; then
    echo "--playlist is required."
    print_usage
    exit $EXIT_CODE_EXCEPTION
fi

if [ ! -e "$playlistFile" ]; then
    echo "File specified by --playlist does not exist: $playlistFile"
    exit $EXIT_CODE_EXCEPTION
fi

if [ -z "$testRootDir" ]; then
    echo "--testRootDir is required."
    print_usage
    exit $EXIT_CODE_EXCEPTION
fi

if [ ! -d "$testRootDir" ]; then
    echo "Directory specified by --testRootDir does not exist: $testRootDir"
    exit $EXIT_CODE_EXCEPTION
fi

if [ ! -z "$testEnv" ] && [ ! -e "$testEnv" ]; then
    echo "File specified by --playlist does not exist: $testEnv"
    exit $EXIT_CODE_EXCEPTION
fi

export CORE_ROOT="$coreOverlayDir"
export __TestEnv=$testEnv


# Variables for running tests in the background
if [ `uname` = "NetBSD" ]; then
    NumProc=$(getconf NPROCESSORS_ONLN)
else
    NumProc=$(getconf _NPROCESSORS_ONLN)
fi

export TIME='<Command time="%U %S %e" mem="%M %t %K" swap="%W %c %w" fault="%F %R %k %r %s" IO="%I %O" exit="%x"/>'

export -f prep_test
export -f run_test

cd $testRootDir
time_start=$(date +"%s")

rm -rf results.int/* results.int

echo "Prepping tests $(date +%r)"
xargs -L 1 -P $NumProc -I{} bash -c prep_test  {} < $playlistFile


trap cleanup EXIT
mkdir results.int
echo $$ > results.int/pid
cp $testEnv results.int

trap handle_ctrl_c INT TERM

xargs -L 1 -P $NumProc -I{} bash -c 'run_test "{}" >/dev/null 2>&1' < $playlistFile &

while true
do
    update_results
    if (( countIncompleteTests == 0 )) ;
    then
        break
    fi
    sleep 60
    jobs -p > /dev/null
done

print_elapsed_time
