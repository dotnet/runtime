#!/usr/bin/env bash

wait_on_pids()
{
  # Wait on the last processes
  for job in $1
  do
    wait $job
    if [ "$?" -ne 0 ]
    then
      TestsFailed=$(($TestsFailed+1))
    fi
  done
}

usage()
{
    echo "Runs .NET CoreFX tests on FreeBSD, NetBSD or Linux"
    echo "usage: run-test [options]"
    echo
    echo "Input sources:"
    echo "    --runtime <location>              Location of root of the binaries directory"
    echo "                                      containing the FreeBSD, NetBSD or Linux runtime"
    echo "                                      default: <repo_root>/bin/testhost/netcoreapp-<OS>-<ConfigurationGroup>-<Arch>"
    echo "    --corefx-tests <location>         Location of the root binaries location containing"
    echo "                                      the tests to run"
    echo "                                      default: <repo_root>/artifacts/bin"
    echo
    echo "Flavor/OS/Architecture options:"
    echo "    --configurationGroup <config>     ConfigurationGroup to run (Debug/Release)"
    echo "                                      default: Debug"
    echo "    --os <os>                         OS to run (FreeBSD, NetBSD or Linux)"
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
    exit 1
}

# Handle Ctrl-C.
function handle_ctrl_c {
  local errorSource='handle_ctrl_c'

  echo ""
  echo "Cancelling test execution."
  exit $TestsFailed
}

# Register the Ctrl-C handler
trap handle_ctrl_c INT

ProjectRoot="$(dirname "$(dirname "$(realpath ${BASH_SOURCE[0]})")")"

# Location parameters
# OS/ConfigurationGroup defaults
ConfigurationGroup="Debug"
OSName=$(uname -s)
case $OSName in
    FreeBSD)
        OS=FreeBSD
        ;;

    NetBSD)
        OS=NetBSD
        ;;

    Linux)
        OS=Linux
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        OS=Linux
        ;;
esac

# Use uname to determine what the CPU is.
CPUName=$(uname -p)
# Some Linux platforms report unknown for platform, but the arch for machine.
if [ "$CPUName" == "unknown" ]; then
    CPUName=$(uname -m)
fi

case $CPUName in
    i686)
        echo "Unsupported CPU $CPUName detected, test might not succeed!"
        __Arch=x86
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
    amd64)
        __Arch=x64
        ;;
    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        __Arch=x64
        ;;
esac

# Misc defaults
TestSelection=".*"
TestsFailed=0

ensure_binaries_are_present()
{
  if [ ! -d $Runtime ]
  then
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
     run_test $testFolder &
     pids="$pids $!"
     numberOfProcesses=$(($numberOfProcesses+1))
     if [ "$numberOfProcesses" -ge $maxProcesses ]; then
       wait_on_pids "$pids"
       numberOfProcesses=0
       pids=""
     fi
  done

  # Wait on the last processes
  wait_on_pids "$pids"
  pids=""
}

# $1 is the path to the test folder
run_test()
{
  testProject=`basename $1`

  # Check for project restrictions

  if [[ ! $testProject =~ $TestSelection ]]; then
    echo "Skipping $testProject"
    exit 0
  fi

  if [ -n "$TestExcludeFile" ]; then
    if grep -q $testProject "$TestExcludeFile" ; then
      echo "Excluding $testProject"
      exit 0
    fi
  fi

  dirName="$1/netcoreapp-$OS-$ConfigurationGroup-$__Arch"
  if [ ! -d "$dirName" ]; then
    echo "Nothing to test in $testProject"
    return
  fi

  if [ ! -e "$dirName/RunTests.sh" ]; then
      echo "Cannot find $dirName/RunTests.sh"
      return
  fi

  pushd $dirName > /dev/null

  echo
  echo "Running tests in $dirName"
  echo "${TimeoutTool}./RunTests.sh --runtime-path $Runtime"
  echo
  ${TimeoutTool}./RunTests.sh --runtime-path "$Runtime"
  exitCode=$?

  if [ $exitCode -ne 0 ]
  then
      echo "error: One or more tests failed while running tests from '$fileNameWithoutExtension'.  Exit code $exitCode."
  fi

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
        --configurationGroup)
        ConfigurationGroup=$2
        ;;
        --os)
        OS=$2
        ;;
        --arch)
        __Arch=$2
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
        --timeout)
        TimeoutTime=$2
        ;;
        *)
        ;;
    esac
    shift
done

# Compute paths to the binaries if they haven't already been computed

if [ "$Runtime" == "" ]
then
    Runtime="$ProjectRoot/artifacts/bin/testhost/netcoreapp-$OS-$ConfigurationGroup-$__Arch"
fi

if [ "$CoreFxTests" == "" ]
then
    CoreFxTests="$ProjectRoot/artifacts/bin"
fi

# Check parameters up front for valid values:

if [ ! "$ConfigurationGroup" == "Debug" ] && [ ! "$ConfigurationGroup" == "Release" ]
then
    echo "error: ConfigurationGroup should be Debug or Release"
    exit 1
fi

if [ ! "$OS" == "FreeBSD" ] && [ ! "$OS" == "NetBSD" ] && [ ! "$OS" == "Linux" ]
then
    echo "error: OS should be FreeBSD, NetBSD or Linux"
    exit 1
fi

export CORECLR_SERVER_GC="$serverGC"
export PAL_OUTPUTDEBUGSTRING="1"

if [ "$LANG" == "" ]
then
    export LANG="en_US.UTF-8"
fi

# Is the 'timeout' tool available?
TimeoutTool=
if hash timeout 2>/dev/null ; then
  TimeoutTool="timeout --kill-after=30s $TimeoutTime "
fi

ensure_binaries_are_present

# Walk the directory tree rooted at src bin/tests/$OS.AnyCPU.$ConfigurationGroup/

TestsFailed=0
numberOfProcesses=0

if [ $RunTestSequential -eq 1 ]
then
    maxProcesses=1;
else
    if [ `uname` = "NetBSD" ] || [ `uname` = "FreeBSD" ]; then
      maxProcesses=$(($(getconf NPROCESSORS_ONLN)+1))
    else
      maxProcesses=$(($(getconf _NPROCESSORS_ONLN)+1))
    fi
fi

if [ -n "$TestDirFile" ] || [ -n "$TestDir" ]
then
    run_selected_tests
else
    run_all_tests "$CoreFxTests/tests/"*.Tests
fi

if [ "$TestsFailed" -gt 0 ]
then
    echo "$TestsFailed test(s) failed"
else
    echo "All tests passed."
fi

exit $TestsFailed
