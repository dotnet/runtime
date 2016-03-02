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
    echo "                                      default: <repo_root>/bin/tests/<OS>.AnyCPU.<ConfigurationGroup>"
    echo "    --corefx-native-bins <location>   Location of the FreeBSD, Linux, NetBSD or OSX native corefx binaries"
    echo "                                      default: <repo_root>/bin/<OS>.x64.<ConfigurationGroup>"
    echo
    echo "Flavor/OS options:"
    echo "    --configurationGroup <config>     ConfigurationGroup to run (Debug/Release)"
    echo "                                      default: Debug"
    echo "    --os <os>                         OS to run (FreeBSD, Linux, NetBSD or OSX)"
    echo "                                      default: detect current OS"
    echo
    echo "Execution options:"
    echo "    --restrict-proj <regex>           Run test projects that match regex"
    echo "                                      default: .* (all projects)"
    echo "    --useServerGC                     Enable Server GC for this test run"
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
OverlayDir="$ProjectRoot/bin/tests/$OS.AnyCPU.$ConfigurationGroup/TestOverlay/"

create_test_overlay()
{
  local mscorlibLocation="$MscorlibBins/mscorlib.dll"

  # Make the overlay
    
  rm -rf $OverlayDir
  mkdir -p $OverlayDir
  
  local LowerConfigurationGroup="$(echo $ConfigurationGroup | awk '{print tolower($0)}')"
 
  # Copy the CoreCLR native binaries
  if [ ! -d $CoreClrBins ]
  then
	echo "error: Coreclr $OS binaries not found at $CoreClrBins"
	exit 1
  fi
  cp -r $CoreClrBins/* $OverlayDir
  
  # Then the mscorlib from the upstream build.
  # TODO When the mscorlib flavors get properly changed then 
  if [ ! -f $mscorlibLocation ]
  then
	echo "error: Mscorlib not found at $mscorlibLocation"
	exit 1
  fi
  cp -r $mscorlibLocation $OverlayDir
  
  # Then the native CoreFX binaries
  if [ ! -d $CoreFxNativeBins ]
  then
	echo "error: Corefx native binaries should be built (use build.sh native in root)"
	exit 1
  fi
  cp $CoreFxNativeBins/* $OverlayDir
}

copy_test_overlay()
{
  testDir=$1

  cp -r $OverlayDir/* $testDir/
}


# $1 is the name of the test project
runtest()
{
  testProject=$1

  # Check here to see whether we should run this project

  if grep "UnsupportedPlatforms.*$OS.*" $1 > /dev/null
  then
    echo "Test project file $1 indicates this test is not supported on $OS, skipping"
    exit 0
  fi
  
  # Check for project restrictions
  
  if [[ ! $testProject =~ $TestSelection ]]; then
    echo "Skipping $testProject"
    exit 0
  fi

  # Grab the directory name that would correspond to this test

  lowerOS="$(echo $OS | awk '{print tolower($0)}')"
  fileName="${file##*/}"
  fileNameWithoutExtension="${fileName%.*}"
  testDllName="$fileNameWithoutExtension.dll"
  xunitOSCategory="non$lowerOS"
  xunitOSCategory+="tests"

  dirName="$CoreFxTests/$fileNameWithoutExtension/dnxcore50"

  if [ ! -d "$dirName" ] || [ ! -f "$dirName/$testDllName" ]
  then
    echo "error: Did not find corresponding test dll for $testProject at $dirName/$testDllName"
    exit 1
  fi

  copy_test_overlay $dirName

  pushd $dirName > /dev/null

  # Remove the mscorlib native image, since our current test layout build process
  # uses a windows runtime and so we include the windows native image for mscorlib
  if [ -e mscorlib.ni.dll ]
  then
    rm mscorlib.ni.dll
  fi

  chmod +x ./corerun
  
  # Invoke xunit

  echo
  echo "Running tests in $dirName"
  echo "./corerun xunit.console.netcore.exe $testDllName -xml testResults.xml -notrait category=failing $OuterLoop -notrait category=$xunitOSCategory" -notrait Benchmark=true
  echo
  ./corerun xunit.console.netcore.exe $testDllName -xml testResults.xml -notrait category=failing $OuterLoop -notrait category=$xunitOSCategory -notrait Benchmark=true
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

((serverGC = 0))
OuterLoop="-notrait category=outerloop"

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
        --useServerGC)
        ((serverGC = 1))
        ;;
        --outerloop)
        OuterLoop="-trait category=outerloop"
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
    CoreFxTests="$ProjectRoot/bin/tests/$OS.AnyCPU.$ConfigurationGroup"
fi

if [ "$CoreFxNativeBins" == "" ]
then
    CoreFxNativeBins="$ProjectRoot/bin/$OS.x64.$ConfigurationGroup/Native"
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

export CORECLR_SERVER_GC="$serverGC"
export PAL_OUTPUTDEBUGSTRING="1"

if [ "$LANG" == "" ]
then
    export LANG="en_US.UTF-8"
fi


create_test_overlay

# Walk the directory tree rooted at src bin/tests/$OS.AnyCPU.$ConfigurationGroup/

TestsFailed=0
numberOfProcesses=0

if [ `uname` = "NetBSD" ]; then
  maxProcesses=$(($(getconf NPROCESSORS_ONLN)+1))
else
  maxProcesses=$(($(getconf _NPROCESSORS_ONLN)+1))
fi

TestProjects=($(find . -regex ".*/src/.*/tests/.*\.Tests\.csproj"))
for file in ${TestProjects[@]}
do
  runtest $file &
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
