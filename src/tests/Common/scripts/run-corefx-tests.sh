#!/usr/bin/env bash

usage()
{
    echo "Runs .NET CoreFX tests on FreeBSD, Linux, NetBSD or OSX"
    echo "usage: run-corefx-tests [options]"
    echo
    echo "Input sources:"
    echo "    --runtime <location>              Location of root of the binaries directory"
    echo "                                      containing the FreeBSD, Linux, NetBSD or OSX runtime"
    echo "                                      default: <repo_root>/bin/testhost/netcoreapp-<OS>-<Configuration>-<Arch>"
    echo "    --corefx-tests <location>         Location of the root binaries location containing"
    echo "                                      the tests to run"
    echo "                                      default: <repo_root>/bin"
    echo
    echo "Flavor/OS/Architecture options:"
    echo "    --configuration <config>     Configuration to run (Debug/Release)"
    echo "                                      default: Debug"
    echo "    --os <os>                         OS to run (FreeBSD, Linux, NetBSD, OSX, SunOS)"
    echo "                                      default: detect current OS"
    echo "    --arch <Architecture>             Architecture to run (x64, arm, armel, x86, arm64)"
    echo "                                      default: detect current architecture"
    echo
    echo "Execution options:"
    echo "    --sequential                      Run tests sequentially (default is to run in parallel)."
    echo "    --restrict-proj <regex>           Run test projects that match regex"
    echo "                                      default: .* (all projects)"
    echo "    --useServerGC                     Enable Server GC for this test run"
    echo "    --test-dir <path>                 Run tests only in the specified directory. Path is relative to the directory"
    echo "                                      specified by --corefx-tests"
    echo "    --test-dir-file <path>            Run tests only in the directories specified by the file at <path>. Paths are"
    echo "                                      listed one line, relative to the directory specified by --corefx-tests"
    echo "    --test-exclude-file <path>        Do not run tests in the directories specified by the file at <path>. Paths are"
    echo "                                      listed one line, relative to the directory specified by --corefx-tests"
    echo "    --timeout <time>                  Specify a per-test timeout value (using 'timeout' tool syntax; default is 10 minutes (10m))"
    echo
    echo "Runtime Code Coverage options:"
    echo "    --coreclr-coverage                Optional argument to get coreclr code coverage reports"
    echo "    --coreclr-objs <location>         Location of root of the object directory"
    echo "                                      containing the FreeBSD, Linux, NetBSD, OSX or SunOS coreclr build"
    echo "                                      default: <repo_root>/bin/obj/<OS>.x64.<Configuration"
    echo "    --coreclr-src <location>          Location of root of the directory"
    echo "                                      containing the coreclr source files"
    echo
    exit 1
}

# Handle Ctrl-C.
function handle_ctrl_c {
    local errorSource='handle_ctrl_c'

    echo ""
    echo "Cancelling test execution."
    exit $countFailedTests
}

# Register the Ctrl-C handler
trap handle_ctrl_c INT

ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Location parameters
# OS/Configuration defaults
Configuration="Debug"

OSName=$(uname -s)
case $OSName in
    Darwin)
        OS=OSX
        ;;

    FreeBSD)
        OS=FreeBSD
        ;;

    Linux)
        OS=Linux
        ;;

    NetBSD)
        OS=NetBSD
        ;;

    SunOS)
        OS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        OS=Linux
        ;;
esac

# Use uname to determine what the CPU is.
CPUName=$(uname -p)

# Some Linux platforms report unknown for platform, but the arch for machine.
if [[ "$CPUName" == "unknown" ]]; then
    CPUName=$(uname -m)
fi

case $CPUName in
    i686)
        echo "Unsupported CPU $CPUName detected, test might not succeed!"
        __Arch=x86
        ;;

    amd64)
        __Arch=x64
        ;;

    x86_64)
        __Arch=x64
        ;;

    armv7l)
        __Arch=armel
        ;;

    aarch64)
        __Arch=arm64
        ;;

    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        __Arch=x64
        ;;
esac

# Misc defaults
TestSelection=".*"
countTotalTests=0
countPassedTests=0
countFailedTests=0

((processCount = 0))
declare -a testFolders
declare -a failedTests
declare -a outputFilePaths
declare -a processIds
waitProcessIndex=
pidNone=0

function waitany {
    local pid
    local exitcode
    while true; do
        for (( i=0; i<$maxProcesses; i++ )); do
            pid=${processIds[$i]}
            if [[ -z "$pid" || "$pid" == "$pidNone" ]]; then
                continue
            fi
            if ! kill -0 $pid 2>/dev/null; then
                wait $pid
                exitcode=$?
                waitProcessIndex=$i
                processIds[$i]=$pidNone
                return $exitcode
            fi
        done
        sleep 0.1
    done
}

function get_available_process_index {
    local pid
    local i=0
    for (( i=0; i<$maxProcesses; i++ )); do
        pid=${processIds[$i]}
        if [[ -z "$pid" || "$pid" == "$pidNone" ]]; then
            break
        fi
    done
    echo $i
}

function finish_test {
    waitany
    local testScriptExitCode=$?
    local finishedProcessIndex=$waitProcessIndex
    ((--processCount))

    local testFolder=${testFolders[$finishedProcessIndex]}
    local testProject=$(basename "$testFolder")

    local outputFilePath=${outputFilePaths[$finishedProcessIndex]}

    echo ">>>>> ${testFolder}"
    cat ${outputFilePath}
    echo "<<<<<"

    if [ $testScriptExitCode -ne 0 ]; then
        failedTests[$countFailedTests]=$testProject
        countFailedTests=$(($countFailedTests+1))
    else
        countPassedTests=$(($countPassedTests+1))
    fi

    let countTotalTests++
}

function finish_remaining_tests {
    # Finish the remaining tests in the order in which they were started
    while ((processCount > 0)); do
        finish_test
    done
}

function start_test {
    local testFolder=$1

    # Check for project restrictions

    testProject=`basename $1`

    if [[ ! $testProject =~ $TestSelection ]]; then
        echo "===== Skipping $testProject"
        return
    fi

    if [ -n "$TestExcludeFile" ]; then
        if grep -q $testProject "$TestExcludeFile" ; then
            echo "===== Excluding $testProject"
            return
        fi
    fi

    dirName="$1/netcoreapp-$OS-$Configuration-$__Arch"
    if [ ! -d "$dirName" ]; then
        echo "===== Nothing to test in $testProject"
        return
    fi

    if [ ! -e "$dirName/RunTests.sh" ]; then
        echo "===== Cannot find $dirName/RunTests.sh"
        return
    fi

    local nextProcessIndex=$(get_available_process_index)

    if ((nextProcessIndex == maxProcesses)); then
        finish_test
        nextProcessIndex=$(get_available_process_index)
    fi

    testFolders[$nextProcessIndex]=$testFolder

    outputFilePath="${testFolder}/output.txt"
    outputFilePaths[$nextProcessIndex]=$outputFilePath

    echo "===== Starting process [${nextProcessIndex}] folder ${testFolder}"

    run_test "$dirName" >"$outputFilePath" 2>&1 &
    processIds[$nextProcessIndex]=$!

    ((++processCount))
}

function summarize_test_run {
    echo ""
    echo "======================="
    echo "     Test Results"
    echo "======================="
    echo "# Tests Run        : $countTotalTests"
    echo "# Passed           : $countPassedTests"
    echo "# Failed           : $countFailedTests"
    echo "======================="

    if [ $countFailedTests -gt 0 ]; then
        echo
        echo "===== Failed tests:"
        for (( i=0; i<$countFailedTests; i++ )); do
            testProject=${failedTests[$i]}
            echo "=====     $testProject"
        done
    fi
}

ensure_binaries_are_present()
{
    if [ ! -d $Runtime ]; then
        echo "error: Coreclr $OS binaries not found at $Runtime"
        exit 1
    fi
}

# $1 is the path of list file
read_array()
{
    local theArray=()

    while IFS='' read -r line || [ -n "$line" ]; do
        theArray[${#theArray[@]}]=$line
    done < "$1"
    echo ${theArray[@]}
}

run_selected_tests()
{
    local selectedTests=()

    if [ -n "$TestDirFile" ]; then
        selectedTests=($(read_array "$TestDirFile"))
    fi

    if [ -n "$TestDir" ]; then
        selectedTests[${#selectedTests[@]}]="$TestDir"
    fi

    run_all_tests ${selectedTests[@]/#/$CoreFxTests/}
}

# $1 is the name of the platform folder (e.g Unix.AnyCPU.Debug)
run_all_tests()
{
    for testFolder in $@
    do
        start_test $testFolder
    done

    finish_remaining_tests
}

# $1 is the path to the folder with the RunTests.sh script in the test folder.
# run_test is run in a sub-process.
run_test()
{
    local dirName="$1"

    pushd $dirName > /dev/null

    echo
    echo "Running tests in $dirName"
    echo "${TimeoutTool}./RunTests.sh --runtime-path $Runtime --rsp-file $ExclusionRspFile"
    echo
    ${TimeoutTool}./RunTests.sh --runtime-path "$Runtime" --rsp-file "$ExclusionRspFile"
    exitCode=$?

    if [ $exitCode -ne 0 ]; then
        echo "error: One or more tests failed while running tests from '$fileNameWithoutExtension'.  Exit code $exitCode."
    fi

    popd > /dev/null
    exit $exitCode
}

coreclr_code_coverage()
{
    if [[ "$OS" != "FreeBSD" && "$OS" != "Linux" && "$OS" != "NetBSD" && "$OS" != "OSX" && "$OS" != "SunOS" ]]; then
        echo "error: Code Coverage not supported on $OS"
        exit 1
    fi

    if [[ -z "$CoreClrSrc" ]]; then
        echo "error: Coreclr source files are required to generate code coverage reports"
        echo "Coreclr source files root path can be passed using '--coreclr-src' argument"
        exit 1
    fi

    local coverageDir="$ProjectRoot/bin/Coverage"
    local toolsDir="$ProjectRoot/bin/Coverage/tools"
    local reportsDir="$ProjectRoot/bin/Coverage/reports"
    local packageName="unix-code-coverage-tools.1.0.0.nupkg"
    rm -rf $coverageDir
    mkdir -p $coverageDir
    mkdir -p $toolsDir
    mkdir -p $reportsDir
    pushd $toolsDir > /dev/null

    echo "Pulling down code coverage tools"

    which curl > /dev/null 2> /dev/null
    if [ $? -ne 0 ]; then
        wget -q -O $packageName https://www.myget.org/F/dotnet-buildtools/api/v2/package/unix-code-coverage-tools/1.0.0
    else
        curl -sSL -o $packageName https://www.myget.org/F/dotnet-buildtools/api/v2/package/unix-code-coverage-tools/1.0.0
    fi

    echo "Unzipping to $toolsDir"
    unzip -q -o $packageName

    # Invoke gcovr
    chmod a+rwx ./gcovr
    chmod a+rwx ./$OS/llvm-cov

    echo
    echo "Generating coreclr code coverage reports at $reportsDir/coreclr.html"
    echo "./gcovr $CoreClrObjs --gcov-executable=$toolsDir/$OS/llvm-cov -r $CoreClrSrc --html --html-details -o $reportsDir/coreclr.html"
    echo
    ./gcovr $CoreClrObjs --gcov-executable=$toolsDir/$OS/llvm-cov -r $CoreClrSrc --html --html-details -o $reportsDir/coreclr.html
    exitCode=$?
    popd > /dev/null
    exit $exitCode
}

# Parse arguments

RunTestSequential=0
((serverGC = 0))
TimeoutTime=20m

while [[ $# > 0 ]]
do
    opt="$1"
    case $opt in
        -h|--help)
            usage
            ;;

        --runtime)
            Runtime=$2
            ;;

        --corefx-tests)
            CoreFxTests=$2
            ;;

        --restrict-proj)
            TestSelection=$2
            ;;

        --configuration)
            Configuration=$2
            ;;

        --os)
            OS=$2
            ;;

        --arch)
            __Arch=$2
            ;;

        --coreclr-coverage)
            CoreClrCoverage=ON
            ;;

        --coreclr-objs)
            CoreClrObjs=$2
            ;;

        --coreclr-src)
            CoreClrSrc=$2
            ;;

        --sequential)
            RunTestSequential=1
            ;;

        --useServerGC)
            ((serverGC = 1))
            ;;

        --test-dir)
            TestDir=$2
            ;;

        --test-dir-file)
            TestDirFile=$2
            ;;

        --test-exclude-file)
            TestExcludeFile=$2
            ;;

        --exclusion-rsp-file)
            ExclusionRspFile=$2
            ;;

        --timeout)
            TimeoutTime=$2
            ;;

        --outerloop)
            OuterLoop=""
            ;;

        --IgnoreForCI)
            IgnoreForCI="-notrait category=IgnoreForCI"
            ;;

        *)
            ;;
    esac
    shift
done

# Compute paths to the binaries if they haven't already been computed

if [[ -z "$Runtime" ]]; then
    Runtime="$ProjectRoot/bin/testhost/netcoreapp-$OS-$Configuration-$__Arch"
fi

if [[ -z "$CoreFxTests" ]]; then
    CoreFxTests="$ProjectRoot/bin"
fi

# Check parameters up front for valid values:

if [[ "$Configuration" != "Debug" && "$Configuration" != "Release" ]]; then
    echo "error: Configuration should be Debug or Release"
    exit 1
fi

if [[ "$OS" != "FreeBSD" && "$OS" != "Linux" && "$OS" != "NetBSD" && "$OS" != "OSX" && "$OS" != "SunOS" ]]; then
    echo "error: OS should be FreeBSD, Linux, NetBSD, OSX or SunOS"
    exit 1
fi

export CORECLR_SERVER_GC="$serverGC"
export PAL_OUTPUTDEBUGSTRING="1"

if [[ -z "$LANG" ]]; then
    export LANG="en_US.UTF-8"
fi

# Is the 'timeout' tool available?
TimeoutTool=
if hash timeout 2>/dev/null ; then
    TimeoutTool="timeout --kill-after=30s $TimeoutTime "
fi

ensure_binaries_are_present

# Walk the directory tree rooted at src bin/tests/$OS.AnyCPU.$Configuration/

numberOfProcesses=0

# Variables for running tests in the background
if [ `uname` = "NetBSD" ]; then
    NumProc=$(getconf NPROCESSORS_ONLN)
elif [ `uname` = "Darwin" ]; then
    NumProc=$(getconf _NPROCESSORS_ONLN)
else
    if [ -x "$(command -v nproc)" ]; then
        NumProc=$(nproc --all)
    elif [ -x "$(command -v getconf)" ]; then
        NumProc=$(getconf _NPROCESSORS_ONLN)
    else
        NumProc=1
    fi
fi
maxProcesses=$NumProc

if [ $RunTestSequential -eq 1 ]; then
    maxProcesses=1
fi

if [[ -n "$TestDirFile" || -n "$TestDir" ]]; then
    run_selected_tests
else
    run_all_tests "$CoreFxTests/tests/"*.Tests
fi

if [[ "$CoreClrCoverage" == "ON" ]]; then
    coreclr_code_coverage
fi

summarize_test_run

exit $countFailedTests
