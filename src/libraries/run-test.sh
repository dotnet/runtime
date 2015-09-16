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
    echo "Runs tests on linux/mac that don't have native build support"
    echo "usage: run-test [options]"
    echo
    echo "Input sources:"
    echo "    --coreclr-bins <location>         Location of root of the binaries directory"
    echo "                                      containing the linux/mac coreclr build"
    echo "                                      default: <repo_root>/bin/Product/<OS>.x64.<Configuration>"
    echo "    --mscorlib-bins <location>        Location of the root binaries directory containing"
    echo "                                      the linux/mac mscorlib.dll"
    echo "                                      default: <repo_root>/bin/Product/<OS>.x64.<Configuration>"
    echo "    --corefx-tests <location>         Location of the root binaries location containing"
    echo "                                      the windows tests"
    echo "                                      default: <repo_root>/bin/tests/Windows_NT.AnyCPU.<Configuration>"
    echo "    --corefx-bins <location>          Location of the linux/mac corefx binaries"
    echo "                                      default: <repo_root>/bin/<OS>.AnyCPU.<Configuration>"
    echo "    --corefx-native-bins <location>   Location of the linux/mac native corefx binaries"
    echo "                                      default: <repo_root>/bin/<OS>.x64.<Configuration>"
    echo
    echo "Flavor/OS options:"
    echo "    --configuration <config>          Configuration to run (Debug/Release)"
    echo "                                      default: Debug"
    echo "    --os <os>                         OS to run (OSX/Linux)"
    echo "                                      default: detect current OS"
    echo
    echo "Execution options:"
    echo "    --restrict-proj <regex>       Run test projects that match regex"
    echo "                                  default: .* (all projects)"
    echo
    echo "Runtime Code Coverage options:"
    echo "    --coreclr-coverage                Optional argument to get coreclr code coverage reports"
    echo "    --coreclr-objs <location>         Location of root of the object directory"
    echo "                                      containing the linux/mac coreclr build"
    echo "                                      default: <repo_root>/bin/obj/<OS>.x64.<Configuration>"
    echo "    --coreclr-src <location>          Location of root of the directory"
    echo "                                      containing the coreclr source files"
    echo
    exit 1
}

ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Location parameters
# OS/Configuration defaults
Configuration="Debug"
OSName=$(uname -s)
case $OSName in
    Linux)
        OS=Linux
        ;;

    Darwin)
        OS=OSX
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        OS=Linux
        ;;
esac
# Misc defaults
TestHostVersion="0.0.2-prerelease"
TestSelection=".*"
TestsFailed=0
OverlayDir="$ProjectRoot/bin/tests/$OS.AnyCPU.$Configuration/TestOverlay/"

create_test_overlay()
{
  # Creates the test overlay that will be copied on top of
  # Each of the test directories.

  local packageName="Microsoft.DotNet.CoreFx.$OS.TemporaryTestHost.$TestHostVersion.nupkg"
  local packageDir="packages/Microsoft.DotNet.CoreFx.$OS.TemporaryTestHost.$TestHostVersion"
  rm -rf $packageDir
  mkdir -p $packageDir
  pushd $packageDir > /dev/null
  # Pull down the testhost package and unzip it.
  echo "Pulling down $packageName"
  wget -q https://www.myget.org/F/dotnet-buildtools/api/v2/package/Microsoft.DotNet.CoreFx.$OS.TemporaryTestHost/$TestHostVersion -O $packageName
  echo "Unzipping to $packageDir"
  unzip -q -o $packageName
  popd > /dev/null

  # Make the overlay

  rm -rf $OverlayDir
  mkdir -p $OverlayDir
  
  local LowerConfiguration="$(echo $Configuration | awk '{print tolower($0)}')"

  # First the temporary test host binaries
  local packageLibDir="$packageDir/lib"
  local mscorlibLocation="$MscorlibBins/mscorlib.dll"
  
  if [ ! -d $packageLibDir ]
  then
	echo "Package not laid out as expected"
	exit 1
  fi
  cp $packageLibDir/* $OverlayDir
  
  # Then the CoreCLR native binaries
  if [ ! -d $CoreClrBins ]
  then
	echo "Coreclr $OS binaries not found at $CoreClrBins"
	exit 1
  fi
  cp -r $CoreClrBins/* $OverlayDir
  
  # Then the mscorlib from the upstream build.
  # TODO When the mscorlib flavors get properly changed then 
  if [ ! -f $mscorlibLocation ]
  then
	echo "Mscorlib not found at $mscorlibLocation"
	exit 1
  fi
  cp -r $mscorlibLocation $OverlayDir
  
  # Then the binaries from the linux build of corefx
  if [ ! -d $CoreFxBins ]
  then
	echo "Corefx binaries not found at $CoreFxBins"
	exit 1
  fi
  find $CoreFxBins -path "*ToolRuntime*" -prune -o -name '*.dll' -and -not -name "*Test*" -exec cp '{}' "$OverlayDir" ";"

  # Then the native CoreFX binaries
  if [ ! -d $CoreFxNativeBins ]
  then
	echo "Corefx native binaries should be built (use build.sh in root)"
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

  if grep "UnsupportedPlatforms.*$OS.*" $1
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
    echo "Did not find corresponding test dll for $testProject at $dirName/$testDllName"
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
  
  # Invoke xunit

  echo
  echo "Running tests in $dirName"
  echo "./corerun xunit.console.netcore.exe $testDllName -xml testResults.xml -notrait category=failing -notrait category=OuterLoop -notrait category=$xunitOSCategory"
  echo
  ./corerun xunit.console.netcore.exe $testDllName -xml testResults.xml -notrait category=failing -notrait category=OuterLoop -notrait category=$xunitOSCategory
  exitCode=$?
  popd > /dev/null
  exit $exitCode
}

coreclr_code_coverage()
{
  if [ ! "$OS" == "OSX" ] && [ ! "$OS" == "Linux" ]
  then
      echo "Code Coverage not supported on $OS"
      exit 1
  fi

  if [ "$CoreClrSrc" == "" ]
    then
      echo "Coreclr source files are required to generate code coverage reports"
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
  wget -q https://www.myget.org/F/dotnet-buildtools/api/v2/package/unix-code-coverage-tools/1.0.0 -O $packageName
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
        --corefx-bins)
        CoreFxBins=$2
        ;;
        --corefx-native-bins)
        CoreFxNativeBins=$2
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
        --coreclr-coverage)
        CoreClrCoverage=ON
        ;;
        --coreclr-objs)
        CoreClrObjs=$2
        ;;
        --coreclr-src)
        CoreClrSrc=$2
        ;;
        *)
        ;;
    esac
    shift
done

# Compute paths to the binaries if they haven't already been computed

if [ "$CoreClrBins" == "" ]
then
    CoreClrBins="$ProjectRoot/bin/Product/$OS.x64.$Configuration"
fi

if [ "$MscorlibBins" == "" ]
then
    MscorlibBins="$ProjectRoot/bin/Product/$OS.x64.$Configuration"
fi

if [ "$CoreFxTests" == "" ]
then
    CoreFxTests="$ProjectRoot/bin/tests/Windows_NT.AnyCPU.$Configuration"
fi

if [ "$CoreFxBins" == "" ]
then
    CoreFxBins="$ProjectRoot/bin/$OS.AnyCPU.$Configuration"
fi

if [ "$CoreFxNativeBins" == "" ]
then
    CoreFxNativeBins="$ProjectRoot/bin/$OS.x64.$Configuration/Native"
fi

# Check parameters up front for valid values:

if [ ! "$Configuration" == "Debug" ] && [ ! "$Configuration" == "Release" ]
then
    echo "Configuration should be Debug or Release"
    exit 1
fi

if [ ! "$OS" == "OSX" ] && [ ! "$OS" == "Linux" ]
then
    echo "OS should be Linux or OSX"
    exit 1
fi

if [ "$CoreClrObjs" == "" ]
then
    CoreClrObjs="$ProjectRoot/bin/obj/$OS.x64.$Configuration"
fi


create_test_overlay

# Walk the directory tree rooted at src bin/tests/Windows_NT.AnyCPU.$Configuration/

TestsFailed=0
numberOfProcesses=0
maxProcesses=$(($(getconf _NPROCESSORS_ONLN)+1))
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
fi
exit $TestsFailed

