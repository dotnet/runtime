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
    echo "Runs .NET CoreFX tests on FreeBSD, Linux, NetBSD or OSX"
    echo "usage: run-test [options]"
    echo
    echo "Input sources:"
    echo "    --coreclr-bins <location>         Location of root of the binaries directory"
    echo "                                      containing the FreeBSD, Linux, NetBSD or OSX coreclr build"
    echo "                                      default: <repo_root>/bin/Product/<OS>.x64.<ConfigurationGroup>"
    echo "    --mscorlib-bins <location>        Location of the root binaries directory containing"
    echo "                                      the FreeBSD, Linux, NetBSD or OSX mscorlib.dll"
    echo "                                      default: <repo_root>/bin/Product/<OS>.x64.<ConfigurationGroup>"
    echo "    --corefx-tests <location>         Location of the root binaries location containing"
    echo "                                      the tests to run"
    echo "                                      default: <repo_root>/bin/tests"
    echo "    --corefx-native-bins <location>   Location of the FreeBSD, Linux, NetBSD or OSX native corefx binaries"
    echo "                                      default: <repo_root>/bin/<OS>.x64.<ConfigurationGroup>"
    echo "    --corefx-packages <location>      Location of the packages restored from NuGet."
    echo "                                      default: <repo_root>/packages"
    echo "    --testRelPath <path>              Relative path to test script"
    echo "                                      Path is relative from the directory specified by project name"
    echo "                                      default: default.netcoreapp1.1"
    echo
    echo "Flavor/OS options:"
    echo "    --configurationGroup <config>     ConfigurationGroup to run (Debug/Release)"
    echo "                                      default: Debug"
    echo "    --os <os>                         OS to run (FreeBSD, Linux, NetBSD or OSX)"
    echo "                                      default: detect current OS"
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
    echo
    echo "Runtime Code Coverage options:"
    echo "    --coreclr-coverage                Optional argument to get coreclr code coverage reports"
    echo "    --coreclr-objs <location>         Location of root of the object directory"
    echo "                                      containing the FreeBSD, Linux, NetBSD or OSX coreclr build"
    echo "                                      default: <repo_root>/bin/obj/<OS>.x64.<ConfigurationGroup"
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
  exit $TestsFailed
}

# Register the Ctrl-C handler
trap handle_ctrl_c INT

ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Location parameters
# OS/ConfigurationGroup defaults
ConfigurationGroup="Debug"
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

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        OS=Linux
        ;;
esac
# Misc defaults
TestSelection=".*"
TestsFailed=0

# TestRelPath default
TestRelPath="default.netcoreapp1.1"

ensure_binaries_are_present()
{
  local LowerConfigurationGroup="$(echo $ConfigurationGroup | awk '{print tolower($0)}')"

  # Copy the CoreCLR native binaries
  if [ ! -d $CoreClrBins ]
  then
	echo "error: Coreclr $OS binaries not found at $CoreClrBins"
	exit 1
  fi

  # Then the mscorlib from the upstream build.
  # TODO When the mscorlib flavors get properly changed then
  if [ ! -f $MscorlibBins/mscorlib.dll ]
  then
	echo "error: Mscorlib not found at $MscorlibBins"
	exit 1
  fi

  # Then the native CoreFX binaries
  if [ ! -d $CoreFxNativeBins ]
  then
	echo "error: Corefx native binaries should be built (use build.sh native in root)"
	exit 1
  fi
}

copy_test_overlay()
{
  testDir=$1

  link_files_in_directory "$CoreClrBins" "$testDir"
  link_files_in_directory "$CoreFxNativeBins" "$testDir"

  ln -f $MscorlibBins/mscorlib.dll $testDir/mscorlib.dll

  # If we have a native image for mscorlib, copy it as well.
  if [ -f $MscorlibBins/mscorlib.ni.dll ]
  then
      ln -f  $MscorlibBins/mscorlib.ni.dll $testDir/mscorlib.ni.dll
  fi
}

# $1 is the source directory
# $2 is the destination directory
link_files_in_directory()
{
    for path in `find $1 -maxdepth 1 -type f`; do
      fileName=`basename $path`
      ln -f $path "$2/$fileName"
    done
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

  dirName="$1/$TestRelPath"
  copy_test_overlay $dirName

  pushd $dirName > /dev/null

  chmod +x ./RunTests.sh
  chmod +x ./corerun

  echo
  echo "Running tests in $dirName"
  echo "./RunTests.sh $CoreFxPackages"
  echo
  ./RunTests.sh "$CoreFxPackages"
  exitCode=$?

  if [ $exitCode -ne 0 ]
  then
      echo "error: One or more tests failed while running tests from '$fileNameWithoutExtension'.  Exit code $exitCode."
  fi

  popd > /dev/null
  exit $exitCode
}

coreclr_code_coverage()
{
  if [ ! "$OS" == "FreeBSD" ] && [ ! "$OS" == "Linux" ] && [ ! "$OS" == "NetBSD" ] && [ ! "$OS" == "OSX" ]
  then
      echo "error: Code Coverage not supported on $OS"
      exit 1
  fi

  if [ "$CoreClrSrc" == "" ]
    then
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

while [[ $# > 0 ]]
do
    opt="$1"
    case $opt in
        -h|--help)
        usage
        ;;
        --coreclr-bins)
        CoreClrBins=$2
        ;;
        --mscorlib-bins)
        MscorlibBins=$2
        ;;
        --corefx-tests)
        CoreFxTests=$2
        ;;
        --corefx-native-bins)
        CoreFxNativeBins=$2
        ;;
        --corefx-packages)
        CoreFxPackages=$2
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
        --testRelPath)
        TestRelPath=$2
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

if [ "$CoreClrBins" == "" ]
then
    CoreClrBins="$ProjectRoot/bin/Product/$OS.x64.$ConfigurationGroup"
fi

if [ "$MscorlibBins" == "" ]
then
    MscorlibBins="$ProjectRoot/bin/Product/$OS.x64.$ConfigurationGroup"
fi

if [ "$CoreFxTests" == "" ]
then
    CoreFxTests="$ProjectRoot/bin/tests"
fi

if [ "$CoreFxNativeBins" == "" ]
then
    CoreFxNativeBins="$ProjectRoot/bin/$OS.x64.$ConfigurationGroup/Native"
fi

if [ "$CoreFxPackages" == "" ]
then
    CoreFxPackages="$ProjectRoot/packages"
fi

# Check parameters up front for valid values:

if [ ! "$ConfigurationGroup" == "Debug" ] && [ ! "$ConfigurationGroup" == "Release" ]
then
    echo "error: ConfigurationGroup should be Debug or Release"
    exit 1
fi

if [ ! "$OS" == "FreeBSD" ] && [ ! "$OS" == "Linux" ] && [ ! "$OS" == "NetBSD" ] && [ ! "$OS" == "OSX" ]
then
    echo "error: OS should be FreeBSD, Linux, NetBSD or OSX"
    exit 1
fi

if [ "$CoreClrObjs" == "" ]
then
    CoreClrObjs="$ProjectRoot/bin/obj/$OS.x64.$ConfigurationGroup"
fi

# The CI system shares PR build job definitions between RC2 and master.  In RC2, we expected
# that CoreFxTests was the path to the root folder containing the tests for a specific platform
# (since all tests were rooted under a path like tests/Linux.AnyCPU.$ConfigurationGroup). In
# master, we instead want CoreFxTests to point at the root of the tests folder, since tests
# are now split across tests/AnyOS.AnyCPU.$ConfigruationGroup,
# tests/Unix.AnyCPU.$ConfigruationGroup and tests/$OS.AnyCPU.$ConfigurationGroup.
#
# Until we can split the CI definitions up, we need them to pass a platform specific folder (so
# the jobs work on RC2), so here we detect that case and use the parent folder instead.
if [[ `basename $CoreFxTests` =~ ^(Linux|OSX|FreeBSD|NetBSD) ]]
then
    CoreFxTests=`dirname $CoreFxTests`
fi

export CORECLR_SERVER_GC="$serverGC"
export PAL_OUTPUTDEBUGSTRING="1"

if [ "$LANG" == "" ]
then
    export LANG="en_US.UTF-8"
fi

ensure_binaries_are_present

# Walk the directory tree rooted at src bin/tests/$OS.AnyCPU.$ConfigurationGroup/

TestsFailed=0
numberOfProcesses=0

if [ $RunTestSequential -eq 1 ]
then
    maxProcesses=1;
else
    if [ `uname` = "NetBSD" ]; then
      maxProcesses=$(($(getconf NPROCESSORS_ONLN)+1))
    else
      maxProcesses=$(($(getconf _NPROCESSORS_ONLN)+1))
    fi
fi

if [ -n "$TestDirFile" ] || [ -n "$TestDir" ]
then
    run_selected_tests
else
    run_all_tests "$CoreFxTests/AnyOS.AnyCPU.$ConfigurationGroup/"*
    run_all_tests "$CoreFxTests/Unix.AnyCPU.$ConfigurationGroup/"*
    run_all_tests "$CoreFxTests/$OS.AnyCPU.$ConfigurationGroup/"*
fi

if [ "$CoreClrCoverage" == "ON" ]
then
    coreclr_code_coverage
fi

if [ "$TestsFailed" -gt 0 ]
then
    echo "$TestsFailed test(s) failed"
else
    echo "All tests passed."
fi

exit $TestsFailed
