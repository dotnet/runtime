#!/usr/bin/env bash

function print_usage {
    echo ""
    echo "CoreCLR test runner script."
    echo "Arguments:"
    echo "  -v, --verbose        : Show output from each test."
    echo "  --testDirFile=<path> : Run tests only in the directories specified by the file at <path>."
    echo "                         The file should specify one directory per line."
    echo ""
}

function print_results {
    echo ""
    echo "======================="
    echo "     Test Results"
    echo "======================="
    echo "# Tests Discovered : "$countTotalTests
    echo "# Passed           : "$countPassedTests
    echo "# Failed           : "$countFailedTests
    echo "# Skipped          : "$countSkippedTests
    echo "======================="
}

# Handle Ctrl-C. We will stop execution and print the results that
# we gathered so far.
function handle_ctrl_c {
    echo ""
    echo "*** Stopping... ***"
    print_results
    exit 0
}

# Register the Ctrl-C handler
trap handle_ctrl_c INT

# Get a list of directories in which to scan for tests by reading the
# specified file line by line.
function set_test_directories {
    listFileName=$1

    if [ ! -f $listFileName ]
    then
        echo "Test directories file not found at "$listFileName
        echo "Exiting..."
        exit 1
    fi

    readarray testDirectories < $listFileName
}

function run_tests_in_directory {
    rootDir=$1

    # Recursively search through directories for .sh files to run.
    for file in $(find $rootDir -name '*.sh' -printf '%P\n')
    do
        scriptFullPath="$rootDir/$file"

        # Switch to directory where the script is
        cd "$(dirname $scriptFullPath)"

        # Convert DOS line endings to Unix if needed
        sed -i 's/\r$//' $scriptFullPath

        scriptName=$(basename $file)
        test $verbose == 1 && echo "Starting "$file

        # Run the test
        ./$scriptName |
            while testOutput= read -r line
            do
                # Print the test output if verbose mode is on
                test $verbose == 1 && echo "         "$line
            done;

        testScriptExitCode=${PIPESTATUS[0]}
        case $testScriptExitCode in
            0)
                let countPassedTests++
                echo "PASSED    - "$scriptFullPath
                ;;
            1)
                let countFailedTests++
                echo "FAILED    - "$scriptFullPath
                ;;
            2)
                let countSkippedTests++
                echo "SKIPPED   - "$scriptFullPath
                ;;
            esac

        let countTotalTests++

        # Return to root directory
        cd $rootDir
    done
}

# Initialize counters for bookkeeping.
countTotalTests=0
countPassedTests=0
countFailedTests=0
countSkippedTests=0

currDir=`pwd`

# Handle arguments
verbose=0
for i in "$@"
do
    case $i in
    -h|--help)
    print_usage
    exit 0;
    ;;
    -v|--verbose)
    verbose=1
    ;;
    --testDirFile=*)
    set_test_directories ${i#*=}
    ;;
    *);;
    esac
done

if [ -z $testDirectories ]
then
    # No test directories were specified, so run everything in the current 
    # directory and its subdirectories.
    run_tests_in_directory $currDir
else
    # Otherwise, run all the tests in each specified test directory.
    for testDir in "${testDirectories[@]}"
    do
        run_tests_in_directory $currDir/$testDir
    done
fi

print_results
exit 0
