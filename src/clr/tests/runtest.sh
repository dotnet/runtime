#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'CoreCLR test runner script.'
    echo ''
    echo 'Typical command line:'
    echo ''
    echo 'coreclr/tests/runtest.sh'
    echo '    --testRootDir="temp/Windows_NT.x64.Debug"'
    echo '    --testNativeBinDir="coreclr/bin/obj/Linux.x64.Debug/tests"'
    echo '    --coreClrBinDir="coreclr/bin/Product/Linux.x64.Debug"'
    echo '    --mscorlibDir="windows/coreclr/bin/Product/Linux.x64.Debug"'
    echo '    --coreFxBinDir="corefx/bin/Linux.AnyCPU.Debug"'
    echo '    --coreFxNativeBinDir="corefx/bin/Linux.x64.Debug"'
    echo ''
    echo 'Required arguments:'
    echo '  --testRootDir=<path>         : Root directory of the test build (e.g. coreclr/bin/tests/Windows_NT.x64.Debug).'
    echo '  --testNativeBinDir=<path>    : Directory of the native CoreCLR test build (e.g. coreclr/bin/obj/Linux.x64.Debug/tests).'
    echo '  (Also required: Either --coreOverlayDir, or all of the switches --coreOverlayDir overrides)'
    echo ''
    echo 'Optional arguments:'
    echo '  --coreOverlayDir=<path>      : Directory containing core binaries and test dependencies. If not specified, the'
    echo '                                 default is testRootDir/Tests/coreoverlay. This switch overrides --coreClrBinDir,'
    echo '                                 --mscorlibDir, --coreFxBinDir, and --coreFxNativeBinDir.'
    echo '  --coreClrBinDir=<path>       : Directory of the CoreCLR build (e.g. coreclr/bin/Product/Linux.x64.Debug).'
    echo '  --mscorlibDir=<path>         : Directory containing the built mscorlib.dll. If not specified, it is expected to be'
    echo '                                 in the directory specified by --coreClrBinDir.'
    echo '  --coreFxBinDir=<path>        : Directory of the CoreFX build (e.g. corefx/bin/Linux.AnyCPU.Debug).'
    echo '  --coreFxNativeBinDir=<path>  : Directory of the CoreFX native build (e.g. corefx/bin/Linux.x64.Debug).'
    echo '  --testDir=<path>             : Run tests only in the specified directory. The path is relative to the directory'
    echo '                                 specified by --testRootDir. Multiple of this switch may be specified.'
    echo '  --testDirFile=<path>         : Run tests only in the directories specified by the file at <path>. Paths are listed'
    echo '                                 one line, relative to the directory specified by --testRootDir.'
    echo '  --runFailingTestsOnly        : Run only the tests that are disabled on this platform due to unexpected failures.'
    echo '                                 Failing tests are listed in coreclr/tests/failingTestsOutsideWindows.txt, one per'
    echo '                                 line, as paths to .sh files relative to the directory specified by --testRootDir.'
    echo '  --disableEventLogging        : Disable the events logged by both VM and Managed Code'
    echo '  --sequential                 : Run tests sequentially (default is to run in parallel).'
    echo '  -v, --verbose                : Show output from each test.'
    echo '  -h|--help                    : Show usage information.'
    echo '  --useServerGC                : Enable server GC for this test run'
    echo ''
    echo 'Runtime Code Coverage options:'
    echo '  --coreclr-coverage           : Optional argument to get coreclr code coverage reports'
    echo '  --coreclr-objs=<path>        : Location of root of the object directory'
    echo '                                 containing the linux/mac coreclr build'
    echo '  --coreclr-src=<path>         : Location of root of the directory'
    echo '                                 containing the coreclr source files'
    echo '  --coverage-output-dir=<path> : Directory where coverage output will be written to'
    echo ''
}

function print_results {
    echo ""
    echo "======================="
    echo "     Test Results"
    echo "======================="
    echo "# Tests Discovered : $countTotalTests"
    echo "# Passed           : $countPassedTests"
    echo "# Failed           : $countFailedTests"
    echo "# Skipped          : $countSkippedTests"
    echo "======================="
}

# Initialize counters for bookkeeping.
countTotalTests=0
countPassedTests=0
countFailedTests=0
countSkippedTests=0

# Variables for xUnit-style XML output. XML format: https://xunit.github.io/docs/format-xml-v2.html
xunitOutputPath=
xunitTestOutputPath=

# libExtension determines extension for dynamic library files
OSName=$(uname -s)
libExtension=
case $OSName in
    Linux)
        libExtension="so"
        ;;

    Darwin)
        libExtension="dylib"
        ;;
    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        libExtension="so"
        ;;
esac


function xunit_output_begin {
    xunitOutputPath=$testRootDir/coreclrtests.xml
    xunitTestOutputPath=${xunitOutputPath}.test
    if [ -e "$xunitOutputPath" ]; then
        rm -f -r "$xunitOutputPath"
    fi
    if [ -e "$xunitTestOutputPath" ]; then
        rm -f -r "$xunitTestOutputPath"
    fi
}

function xunit_output_add_test {
    # <assemblies>
    #   <assembly>
    #     <collection>
    #       <test .../> <!-- Write this element here -->

    local scriptFilePath=$1
    local outputFilePath=$2
    local testResult=$3 # Pass, Fail, or Skip
    local testScriptExitCode=$4

    local testPath=${scriptFilePath%.sh} # Remove trailing ".sh"
    local testDir=$(dirname "$testPath")
    local testName=$(basename "$testPath")

    # Replace '/' with '.'
    testPath=$(echo "$testPath" | tr / .)
    testDir=$(echo "$testDir" | tr / .)

    local line

    line="      "
    line="${line}<test"
    line="${line} name=\"${testPath}\""
    line="${line} type=\"${testDir}\""
    line="${line} method=\"${testName}\""
    line="${line} result=\"${testResult}\""

    if [ "$testResult" == "Pass" ]; then
        line="${line}/>"
        echo "$line" >>"$xunitTestOutputPath"
        return
    fi

    line="${line}>"
    echo "$line" >>"$xunitTestOutputPath"

    line="        "
    if [ "$testResult" == "Skip" ]; then
        line="${line}<reason><![CDATA[$(cat "$outputFilePath")]]></reason>"
        echo "$line" >>"$xunitTestOutputPath"
    else
        line="${line}<failure exception-type=\"Exit code: ${testScriptExitCode}\">"
        echo "$line" >>"$xunitTestOutputPath"

        line="          "
        line="${line}<message>"
        echo "$line" >>"$xunitTestOutputPath"
        line="            "
        line="${line}<![CDATA["
        echo "$line" >>"$xunitTestOutputPath"
        cat "$outputFilePath" >>"$xunitTestOutputPath"
        line="            "
        line="${line}]]>"
        echo "$line" >>"$xunitTestOutputPath"
        line="          "
        line="${line}</message>"
        echo "$line" >>"$xunitTestOutputPath"

        line="        "
        line="${line}</failure>"
        echo "$line" >>"$xunitTestOutputPath"
    fi

    line="      "
    line="${line}</test>"
    echo "$line" >>"$xunitTestOutputPath"
}

function xunit_output_end {
    local errorSource=$1
    local errorMessage=$2

    local errorCount
    if [ -z "$errorSource" ]; then
        ((errorCount = 0))
    else
        ((errorCount = 1))
    fi

    echo '<?xml version="1.0" encoding="utf-8"?>' >>"$xunitOutputPath"
    echo '<assemblies>' >>"$xunitOutputPath"

    local line

    # <assembly ...>
    line="  "
    line="${line}<assembly"
    line="${line} name=\"CoreClrTestAssembly\""
    line="${line} total=\"${countTotalTests}\""
    line="${line} passed=\"${countPassedTests}\""
    line="${line} failed=\"${countFailedTests}\""
    line="${line} skipped=\"${countSkippedTests}\""
    line="${line} errors=\"${errorCount}\""
    line="${line}>"
    echo "$line" >>"$xunitOutputPath"

    # <collection ...>
    line="    "
    line="${line}<collection"
    line="${line} name=\"CoreClrTestCollection\""
    line="${line} total=\"${countTotalTests}\""
    line="${line} passed=\"${countPassedTests}\""
    line="${line} failed=\"${countFailedTests}\""
    line="${line} skipped=\"${countSkippedTests}\""
    line="${line}>"
    echo "$line" >>"$xunitOutputPath"

    # <test .../> <test .../> ...
    if [ -f "$xunitTestOutputPath" ]; then
        cat "$xunitTestOutputPath" >>"$xunitOutputPath"
        rm -f "$xunitTestOutputPath"
    fi

    # </collection>
    line="    "
    line="${line}</collection>"
    echo "$line" >>"$xunitOutputPath"

    if [ -n "$errorSource" ]; then
        # <errors>
        line="    "
        line="${line}<errors>"
        echo "$line" >>"$xunitOutputPath"

        # <error ...>
        line="      "
        line="${line}<error"
        line="${line} type=\"TestHarnessError\""
        line="${line} name=\"${errorSource}\""
        line="${line}>"
        echo "$line" >>"$xunitOutputPath"

        # <failure .../>
        line="        "
        line="${line}<failure>${errorMessage}</failure>"
        echo "$line" >>"$xunitOutputPath"

        # </error>
        line="      "
        line="${line}</error>"
        echo "$line" >>"$xunitOutputPath"

        # </errors>
        line="    "
        line="${line}</errors>"
        echo "$line" >>"$xunitOutputPath"
    fi

    # </assembly>
    line="  "
    line="${line}</assembly>"
    echo "$line" >>"$xunitOutputPath"

    # </assemblies>
    echo '</assemblies>' >>"$xunitOutputPath"
}

function exit_with_error {
    local errorSource=$1
    local errorMessage=$2
    local printUsage=$3

    if [ -z "$printUsage" ]; then
        ((printUsage = 0))
    fi

    echo "$errorMessage"
    xunit_output_end "$errorSource" "$errorMessage"
    if ((printUsage != 0)); then
        print_usage
    fi
    exit $EXIT_CODE_EXCEPTION
}

# Handle Ctrl-C. We will stop execution and print the results that
# we gathered so far.
function handle_ctrl_c {
    local errorSource='handle_ctrl_c'

    echo ""
    echo "*** Stopping... ***"
    print_results
    exit_with_error "$errorSource" "Test run aborted by Ctrl+C."
}

# Register the Ctrl-C handler
trap handle_ctrl_c INT

function create_core_overlay {
    local errorSource='create_core_overlay'
    local printUsage=1

    if [ -n "$coreOverlayDir" ]; then
        export CORE_ROOT="$coreOverlayDir"
        return
    fi

    # Check inputs to make sure we have enough information to create the core layout. $testRootDir/Tests/Core_Root should
    # already exist and contain test dependencies that are not built.
    local testDependenciesDir=$testRootDir/Tests/Core_Root
    if [ ! -d "$testDependenciesDir" ]; then
        exit_with_error "$errorSource" "Did not find the test dependencies directory: $testDependenciesDir"
    fi
    if [ -z "$coreClrBinDir" ]; then
        exit_with_error "$errorSource" "One of --coreOverlayDir or --coreClrBinDir must be specified." "$printUsage"
    fi
    if [ ! -d "$coreClrBinDir" ]; then
        exit_with_error "$errorSource" "Directory specified by --coreClrBinDir does not exist: $coreClrBinDir"
    fi
    if [ -z "$mscorlibDir" ]; then
        mscorlibDir=$coreClrBinDir
    fi
    if [ ! -f "$mscorlibDir/mscorlib.dll" ]; then
        exit_with_error "$errorSource" "mscorlib.dll was not found in: $mscorlibDir"
    fi
    if [ -z "$coreFxBinDir" ]; then
        exit_with_error "$errorSource" "One of --coreOverlayDir or --coreFxBinDir must be specified." "$printUsage"
    fi
    if [ ! -d "$coreFxBinDir" ]; then
        exit_with_error "$errorSource" "Directory specified by --coreFxBinDir does not exist: $coreFxBinDir"
    fi
    if [ -z "$coreFxNativeBinDir" ]; then
        exit_with_error "$errorSource" "One of --coreOverlayDir or --coreFxBinDir must be specified." "$printUsage"
    fi
    if [ ! -d "$coreFxNativeBinDir/Native" ]; then
        exit_with_error "$errorSource" "Directory specified by --coreFxBinDir does not exist: $coreFxNativeBinDir/Native"
    fi

    # Create the overlay
    coreOverlayDir=$testRootDir/Tests/coreoverlay
    export CORE_ROOT="$coreOverlayDir"
    if [ -e "$coreOverlayDir" ]; then
        rm -f -r "$coreOverlayDir"
    fi
    mkdir "$coreOverlayDir"

    (cd $coreFxBinDir && find . -iname '*.dll' \! -iwholename '*netstandard13aot*'  \! -iwholename '*test*' \! -iwholename '*/ToolRuntime/*' \! -iwholename '*RemoteExecutorConsoleApp*' -exec cp -f '{}' "$coreOverlayDir/" \;)
    cp -f "$coreFxNativeBinDir/Native/"*."$libExtension" "$coreOverlayDir/" 2>/dev/null

    cp -f "$coreClrBinDir/"* "$coreOverlayDir/" 2>/dev/null
    cp -f "$mscorlibDir/mscorlib.dll" "$coreOverlayDir/"
    cp -n "$testDependenciesDir"/* "$coreOverlayDir/" 2>/dev/null
    if [ -f "$coreOverlayDir/mscorlib.ni.dll" ]; then
        rm -f "$coreOverlayDir/mscorlib.ni.dll"
    fi
}

function copy_test_native_bin_to_test_root {
    local errorSource='copy_test_native_bin_to_test_root'

    if [ -z "$testNativeBinDir" ]; then
        exit_with_error "$errorSource" "--testNativeBinDir is required."
    fi
    testNativeBinDir=$testNativeBinDir/src
    if [ ! -d "$testNativeBinDir" ]; then
        exit_with_error "$errorSource" "Directory specified by --testNativeBinDir does not exist: $testNativeBinDir"
    fi

    # Copy native test components from the native test build into the respective test directory in the test root directory
    find "$testNativeBinDir" -type f -iname '*.$libExtension' |
        while IFS='' read -r filePath || [ -n "$filePath" ]; do
            local dirPath=$(dirname "$filePath")
            local destinationDirPath=${testRootDir}${dirPath:${#testNativeBinDir}}
            if [ ! -d "$destinationDirPath" ]; then
                exit_with_error "$errorSource" "Cannot copy native test bin '$filePath' to '$destinationDirPath/', as the destination directory does not exist."
            fi
            cp -f "$filePath" "$destinationDirPath/"
        done
}

# Variables for unsupported and failing tests
declare -a unsupportedTests
declare -a failingTests
((runFailingTestsOnly = 0))

function load_unsupported_tests {
    # Load the list of tests that fail and on this platform. These tests are disabled (skipped), pending investigation.
    # 'readarray' is not used here, as it includes the trailing linefeed in lines placed in the array.
    while IFS='' read -r line || [ -n "$line" ]; do
        unsupportedTests[${#unsupportedTests[@]}]=$line
    done <"$(dirname "$0")/testsUnsupportedOutsideWindows.txt"
}

function load_failing_tests {
    # Load the list of tests that fail and on this platform. These tests are disabled (skipped), pending investigation.
    # 'readarray' is not used here, as it includes the trailing linefeed in lines placed in the array.
    while IFS='' read -r line || [ -n "$line" ]; do
        failingTests[${#failingTests[@]}]=$line
    done <"$(dirname "$0")/testsFailingOutsideWindows.txt"
}

function is_unsupported_test {
    for unsupportedTest in "${unsupportedTests[@]}"; do
        if [ "$1" == "$unsupportedTest" ]; then
            return 0
        fi
    done
    return 1
}

function is_failing_test {
    for failingTest in "${failingTests[@]}"; do
        if [ "$1" == "$failingTest" ]; then
            return 0
        fi
    done
    return 1
}

function skip_unsupported_test {
    # This function runs in a background process. It should not echo anything, and should not use global variables. This
    # function is analogous to run_test, and causes the test to be skipped with the message below.

    local scriptFilePath=$1
    local outputFilePath=$2

    echo "Not supported on this platform." >"$outputFilePath"
    return 2 # skip the test
}

function skip_failing_test {
    # This function runs in a background process. It should not echo anything, and should not use global variables. This
    # function is analogous to run_test, and causes the test to be skipped with the message below.

    local scriptFilePath=$1
    local outputFilePath=$2

    echo "Temporarily disabled on this platform due to unexpected failures." >"$outputFilePath"
    return 2 # skip the test
}

function run_test {
    # This function runs in a background process. It should not echo anything, and should not use global variables.

    local scriptFilePath=$1
    local outputFilePath=$2

    # Switch to directory where the script is
    cd "$(dirname "$scriptFilePath")"

    local scriptFileName=$(basename "$scriptFilePath")
    local outputFileName=$(basename "$outputFilePath")

    # Convert DOS line endings to Unix if needed
    perl -pi -e 's/\r\n|\n|\r/\n/g' "$scriptFileName"
    
    # Add executable file mode bit if needed
    chmod +x "$scriptFileName"

    "./$scriptFileName" >"$outputFileName" 2>&1
    return $?
}

# Variables for running tests in the background
((maxProcesses = $(getconf _NPROCESSORS_ONLN) * 3 / 2)) # long tests delay process creation, use a few more processors
((nextProcessIndex = 0))
((processCount = 0))
declare -a scriptFilePaths
declare -a outputFilePaths
declare -a processIds

function finish_test {
    wait ${processIds[$nextProcessIndex]}
    local testScriptExitCode=$?
    ((--processCount))

    local scriptFilePath=${scriptFilePaths[$nextProcessIndex]}
    local outputFilePath=${outputFilePaths[$nextProcessIndex]}
    local scriptFileName=$(basename "$scriptFilePath")

    local xunitTestResult
    case $testScriptExitCode in
        0)
            let countPassedTests++
            xunitTestResult='Pass'
            if ((verbose == 1 || runFailingTestsOnly == 1)); then
                echo "PASSED   - $scriptFilePath"
            else
                echo "         - $scriptFilePath"
            fi
            ;;
        2)
            let countSkippedTests++
            xunitTestResult='Skip'
            echo "SKIPPED  - $scriptFilePath"
            ;;
        *)
            let countFailedTests++
            xunitTestResult='Fail'
            echo "FAILED   - $scriptFilePath"
            ;;
    esac
    let countTotalTests++

    if ((verbose == 1 || testScriptExitCode != 0)); then
        while IFS='' read -r line || [ -n "$line" ]; do
            echo "               $line"
        done <"$outputFilePath"
    fi

    xunit_output_add_test "$scriptFilePath" "$outputFilePath" "$xunitTestResult" "$testScriptExitCode"
}

function finish_remaining_tests {
    # Finish the remaining tests in the order in which they were started
    if ((nextProcessIndex >= processCount)); then
        ((nextProcessIndex = 0))
    fi
    while ((processCount > 0)); do
        finish_test
        ((nextProcessIndex = (nextProcessIndex + 1) % maxProcesses))
    done
    ((nextProcessIndex = 0))
}

function start_test {
    local scriptFilePath=$1

    if ((runFailingTestsOnly == 1)) && ! is_failing_test "$scriptFilePath"; then
        return
    fi

    if ((nextProcessIndex < processCount)); then
        finish_test
    fi

    scriptFilePaths[$nextProcessIndex]=$scriptFilePath
    local scriptFileName=$(basename "$scriptFilePath")
    local outputFilePath=$(dirname "$scriptFilePath")/${scriptFileName}.out
    outputFilePaths[$nextProcessIndex]=$outputFilePath

    test "$verbose" == 1 && echo "Starting $scriptFilePath"
    if is_unsupported_test "$scriptFilePath"; then
        skip_unsupported_test "$scriptFilePath" "$outputFilePath" &
    elif ((runFailingTestsOnly == 0)) && is_failing_test "$scriptFilePath"; then
        skip_failing_test "$scriptFilePath" "$outputFilePath" &
    else
        run_test "$scriptFilePath" "$outputFilePath" &
    fi
    processIds[$nextProcessIndex]=$!

    ((nextProcessIndex = (nextProcessIndex + 1) % maxProcesses))
    ((++processCount))
}

# Get a list of directories in which to scan for tests by reading the
# specified file line by line.
function set_test_directories {
    local errorSource='set_test_directories'

    local listFileName=$1

    if [ ! -f "$listFileName" ]
    then
        exit_with_error "$errorSource" "Test directories file not found at $listFileName"
    fi

    readarray testDirectories < "$listFileName"
}

function run_tests_in_directory {
    local testDir=$1

    # Recursively search through directories for .sh files to run.
    for scriptFilePath in $(find "$testDir" -type f -iname '*.sh' | sort)
    do
        start_test "${scriptFilePath:2}"
    done
}

function coreclr_code_coverage()
{

  local coverageDir="$coverageOutputDir/Coverage"
  local toolsDir="$coverageOutputDir/Coverage/tools"
  local reportsDir="$coverageOutputDir/Coverage/reports"
  local packageName="unix-code-coverage-tools.1.0.0.nupkg"
  rm -rf $coverageDir
  mkdir -p $coverageDir
  mkdir -p $toolsDir
  mkdir -p $reportsDir
  pushd $toolsDir > /dev/null

  echo "Pulling down code coverage tools"
  wget -q https://www.myget.org/F/dotnet-buildtools/api/v2/package/unix-code-coverage-tools/1.0.0 -O $packageName
  echo "Unzipping to $toolsDir"
  unzip -q -o $packageName

  # Invoke gcovr
  chmod a+rwx ./gcovr
  chmod a+rwx ./$OSName/llvm-cov

  echo
  echo "Generating coreclr code coverage reports at $reportsDir/coreclr.html"
  echo "./gcovr $coreClrObjs --gcov-executable=$toolsDir/$OS/llvm-cov -r $coreClrSrc --html --html-details -o $reportsDir/coreclr.html"
  echo
  ./gcovr $coreClrObjs --gcov-executable=$toolsDir/$OSName/llvm-cov -r $coreClrSrc --html --html-details -o $reportsDir/coreclr.html
  exitCode=$?
  popd > /dev/null
  exit $exitCode
}

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.
readonly EXIT_CODE_EXCEPTION=1     # Script exited because something exceptional happened (e.g. bad arguments, Ctrl-C interrupt).
readonly EXIT_CODE_TEST_FAILURE=2  # Script completed successfully, but one or more tests failed.

# Argument variables
testRootDir=
testNativeBinDir=
coreOverlayDir=
coreClrBinDir=
mscorlibDir=
coreFxBinDir=
coreFxNativeBinDir=
coreClrObjs=
coreClrSrc=
coverageOutputDir=

((disableEventLogging = 0))
((serverGC = 0))

# Handle arguments
verbose=0
for i in "$@"
do
    case $i in
        -h|--help)
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
        -v|--verbose)
            verbose=1
            ;;
        --testRootDir=*)
            testRootDir=${i#*=}
            ;;
        --testNativeBinDir=*)
            testNativeBinDir=${i#*=}
            ;;
        --coreOverlayDir=*)
            coreOverlayDir=${i#*=}
            ;;
        --coreClrBinDir=*)
            coreClrBinDir=${i#*=}
            ;;
        --mscorlibDir=*)
            mscorlibDir=${i#*=}
            ;;
        --coreFxBinDir=*)
            coreFxBinDir=${i#*=}
            ;;
        --coreFxNativeBinDir=*)
            coreFxNativeBinDir=${i#*=}
            ;;
        --testDir=*)
            testDirectories[${#testDirectories[@]}]=${i#*=}
            ;;
        --testDirFile=*)
            set_test_directories "${i#*=}"
            ;;
        --runFailingTestsOnly)
            ((runFailingTestsOnly = 1))
            ;;
        --disableEventLogging)
            ((disableEventLogging = 1))
            ;;
        --sequential)
            ((maxProcesses = 1))
            ;;
        --useServerGC)
            ((serverGC = 1))
            ;;
        --coreclr-coverage)
            CoreClrCoverage=ON
            ;;
        --coreclr-objs=*)
            coreClrObjs=${i#*=}
            ;;
        --coreclr-src=*)
            coreClrSrc=${i#*=}
            ;;
        --coverage-output-dir=*)
            coverageOutputDir=${i#*=}
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

if (( disableEventLogging == 0)); then
        export COMPlus_EnableEventLog=1
fi

export CORECLR_SERVER_GC="$serverGC"

if [ -z "$testRootDir" ]; then
    echo "--testRootDir is required."
    print_usage
    exit $EXIT_CODE_EXCEPTION
fi
if [ ! -d "$testRootDir" ]; then
    echo "Directory specified by --testRootDir does not exist: $testRootDir"
    exit $EXIT_CODE_EXCEPTION
fi

    
# Copy native interop test libraries over to the mscorlib path in
# order for interop tests to run on linux.
cp $mscorlibDir/bin/* $mscorlibDir   

# If this is a coverage run, make sure the appropriate args have been passed
if [ "$CoreClrCoverage" == "ON" ]
then
    echo "Code coverage is enabled for this run"
    echo ""
    if [ ! "$OSName" == "Darwin" ] && [ ! "$OSName" == "Linux" ]
    then
        echo "Code Coverage not supported on $OS"
        exit 1
    fi

    if [ -z "$coreClrObjs" ]
    then
        echo "Coreclr obj files are required to generate code coverage reports"
        echo "Coreclr obj files root path can be passed using '--coreclr-obj' argument"
        exit 1
    fi

    if [ -z "$coreClrSrc" ]
    then
        echo "Coreclr src files are required to generate code coverage reports"
        echo "Coreclr src files root path can be passed using '--coreclr-src' argument"
        exit 1
    fi

    if [ -z "$coverageOutputDir" ]
    then
        echo "Output directory for coverage results must be specified"
        echo "Output path can be specified '--coverage-output-dir' argument"
        exit 1
    fi
fi

xunit_output_begin
create_core_overlay
copy_test_native_bin_to_test_root
load_unsupported_tests
load_failing_tests

if [ -n "$COMPlus_GCStress" ]; then
    scriptPath=$(dirname $0)
    ${scriptPath}/setup-runtime-dependencies.sh --outputDir=$coreOverlayDir
    if [ $? -ne 0 ] 
    then
        echo 'Failed to download coredistools library'
        exit $EXIT_CODE_EXCEPTION
    fi
fi
 
cd "$testRootDir"
if [ -z "$testDirectories" ]
then
    # No test directories were specified, so run everything in the current 
    # directory and its subdirectories.
    run_tests_in_directory "."
else
    # Otherwise, run all the tests in each specified test directory.
    for testDir in "${testDirectories[@]}"
    do
        if [ ! -d "$testDir" ]; then
            echo "Test directory does not exist: $testDir"
        else
            run_tests_in_directory "./$testDir"
        fi
    done
fi
finish_remaining_tests

print_results
xunit_output_end

if [ "$CoreClrCoverage" == "ON" ]
then
    coreclr_code_coverage
fi

if ((countFailedTests > 0)); then
    exit $EXIT_CODE_TEST_FAILURE
fi

exit $EXIT_CODE_SUCCESS
