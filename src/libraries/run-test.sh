#!/usr/bin/env bash

usage()
{
    echo "Runs tests on linux/mac that don't have native build support"
    echo "usage: run-test [options]"
    echo
    echo "Input sources:"
    echo "    --coreclr-bins <location>     Location of root of the binaries directory"
    echo "                                  containing the linux/mac coreclr build"
    echo "                                  default: <repo_root>\binaries"
    echo "    --mscorlib-bins <location>    Location of the root binaries directory containing"
    echo "                                  the linux/mac mscorlib.dll"
    echo "                                  default: <repo_root>\binaries"
    echo "    --corefx-tests <location>     Location of the root binaries location containing"
    echo "                                  the windows tests"
    echo "                                  default: bin"
    echo "    --corefx-bins <location>      Location of the linux/mac corefx binaries"
    echo "                                  default: bin"
    echo
    echo "Flavor/OS options:"
    echo "    --configuration <config>      Configuration to run (Debug/Release)"
    echo "                                  default: Debug"
    echo "    --os <os>                     OS to run (OSX/Linux)"
    echo "                                  default: detect current OS"
    echo
    echo "Execution options:"
    echo "    --restrict-proj <regex>       Run test projects that match regex"
    echo "                                  default: .* (all projects)"
    echo
    exit 1
}

ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
# Location parameters
CoreClrBinRoot="$ProjectRoot/binaries"
MscorlibBinRoot="$ProjectRoot/binaries"
CoreFxTestsRoot="$ProjectRoot/bin"
CoreFxBinRoot="$ProjectRoot/bin"
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
LowerOS="$(echo $OS | awk '{print tolower($OS)}')"
# Misc defaults
TestHostVersion="0.0.1-prerelease"
OverlayDir="$ProjectRoot/bin/tests/$OS.AnyCPU.$Configuration/TestOverlay/"
TestSelection=".*"
TestsFailed=0

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
  
  local LowerConfiguration="$(echo $Configuration | awk '{print tolower($Configuration)}')"

  # First the temporary test host binaries
  local packageLibDir="$packageDir/lib"
  local coreClrDir="$CoreClrBinRoot/Product/$LowerOS.x64.$LowerConfiguration"
  local mscorlibLocation="$MscorlibBinRoot/Product/Unix.x64.$LowerConfiguration/mscorlib.dll"
  local coreFxDir="$CoreFxBinRoot/$OS.AnyCPU.$Configuration"
  
  if [ ! -d $packageLibDir ]
  then
	echo "Package not laid out as expected"
	exit 1
  fi
  cp $packageLibDir/* $OverlayDir
  
  # Then the CoreCLR native binaries
  if [ ! -d $coreClrDir ]
  then
	echo "Coreclr $OS binaries not found at $coreClrDir"
	exit 1
  fi
  cp -r $coreClrDir/* $OverlayDir
  
  # Then the mscorlib from the upstream build.
  # TODO When the mscorlib flavors get properly changed then 
  if [ ! -f $mscorlibLocation ]
  then
	echo "Mscorlib not found at $mscorlibLocation"
	exit 1
  fi
  cp -r $mscorlibLocation $OverlayDir
  
  # Then the binaries from the linux build of corefx
  if [ ! -d $coreFxDir ]
  then
	echo "Mscorlib not found at $mscorlibLocation"
	exit 1
  fi
  cp -n -r $coreFxDir/**/System*.dll $OverlayDir
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

  if grep "UnsupportedOperatingSystems.*$OS.*" $1
  then
    echo "Test project file $1 indicates this test is not supported on $OS, skipping"
    return
  fi
  
  # Check for project restrictions
  
  if [[ ! $testProject =~ $TestSelection ]]; then
    echo "Skipping $testProject"
    return
  fi

  # Grab the directory name that would correspond to this test

  fileName="${file##*/}"
  fileNameWithoutExtension="${fileName%.*}"
  testDllName="$fileNameWithoutExtension.dll"
  xunitOSCategory="non$LowerOStests"

  dirName="$CoreFxTestsRoot/tests/Windows_NT.AnyCPU.$Configuration/$fileNameWithoutExtension/aspnetcore50"

  if [ ! -d "$dirName" ] || [ ! -f "$dirName/$testDllName" ]
  then
    echo "Did not find corresponding test dll for $testProject at $dirName/$testDllName"
    return
  fi

  copy_test_overlay $dirName

  # Invoke xunit

  pushd $dirName > /dev/null
  echo
  echo "Running tests in $dirName"
  echo "./corerun xunit.console.netcore.exe $testDllName -xml testResults.xml -notrait category=failing -notrait category=OuterLoop -notrait category=$xunitOSCategory"
  echo
  ./corerun xunit.console.netcore.exe $testDllName -xml testResults.xml -notrait category=failing -notrait category=OuterLoop -notrait category=$xunitOSCategory
  if [ $? ]
  then
    TestsFailed=1
  fi
  popd > /dev/null
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
        CoreClrBinRoot=$2
        ;;
        --mscorlib-bins)
        MscorlibBinRoot=$2
        ;;
        --corefx-tests)
        CoreFxTestsRoot=$2
        ;;
        --corefx-bins)
        CoreFxBinRoot=$2
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
        *)
        ;;
    esac
    shift
done

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

create_test_overlay

# Walk the directory tree rooted at src bin/tests/Windows_NT.AnyCPU.$Configuration/

for file in src/**/tests/*.Tests.csproj
do
  runtest $file
done

exit $TestsFailed
