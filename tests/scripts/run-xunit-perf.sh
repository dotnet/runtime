#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'CoreCLR perf test script on Linux.'
    echo ''
    echo 'Typical command line:'
    echo ''
    echo 'coreclr/tests/scripts/run-xunit-perf.sh'
    echo '    --testRootDir="temp/Windows_NT.x64.Debug"'
    echo '    --testNativeBinDir="coreclr/bin/obj/Linux.x64.Debug/tests"'
    echo '    --coreClrBinDir="coreclr/bin/Product/Linux.x64.Debug"'
    echo '    --mscorlibDir="windows/coreclr/bin/Product/Linux.x64.Debug"'
    echo '    --coreFxBinDir="corefx/bin/Linux.AnyCPU.Debug"'
    echo ''
    echo 'Required arguments:'
    echo '  --testRootDir=<path>             : Root directory of the test build (e.g. coreclr/bin/tests/Windows_NT.x64.Debug).'
    echo '  --testNativeBinDir=<path>        : Directory of the native CoreCLR test build (e.g. coreclr/bin/obj/Linux.x64.Debug/tests).'
    echo '  (Also required: Either --coreOverlayDir, or all of the switches --coreOverlayDir overrides)'
    echo ''
    echo 'Optional arguments:'
    echo '  --coreOverlayDir=<path>          : Directory containing core binaries and test dependencies. If not specified, the'
    echo '                                     default is testRootDir/Tests/coreoverlay. This switch overrides --coreClrBinDir,'
    echo '                                     --mscorlibDir, and --coreFxBinDir.'
    echo '  --coreClrBinDir=<path>           : Directory of the CoreCLR build (e.g. coreclr/bin/Product/Linux.x64.Debug).'
    echo '  --mscorlibDir=<path>             : Directory containing the built mscorlib.dll. If not specified, it is expected to be'
    echo '                                       in the directory specified by --coreClrBinDir.'
    echo '  --coreFxBinDir="<path>"          : The path to the unpacled runtime folder that is produced as part of a CoreFX build'
	echo '  --uploadToBenchview              : Specify this flag in order to have the results of the run uploaded to Benchview.'
	echo '                                     This also requires that the os flag and runtype flag to be set.  Lastly you must'
	echo '                                     also have the BV_UPLOAD_SAS_TOKEN set to a SAS token for the Benchview upload container'
	echo '  --benchViewOS=<os>               : Specify the os that will be used to insert data into Benchview.'
	echo '  --runType=<private|rolling>      : Specify the runType for Benchview.'
}

# Variables for xUnit-style XML output. XML format: https://xunit.github.io/docs/format-xml-v2.html
xunitOutputPath=
xunitTestOutputPath=

# libExtension determines extension for dynamic library files
OSName=$(uname -s)
libExtension=
case $OSName in
    Darwin)
        libExtension="dylib"
        ;;

    Linux)
        libExtension="so"
        ;;

    NetBSD)
        libExtension="so"
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        libExtension="so"
        ;;
esac

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
    if [ ! -f "$mscorlibDir/mscorlib.dll" ]; then
        exit_with_error "$errorSource" "mscorlib.dll was not found in: $mscorlibDir"
    fi
    if [ -z "$coreFxBinDir" ]; then
        exit_with_error "$errorSource" "One of --coreOverlayDir or --coreFxBinDir must be specified." "$printUsage"
    fi

    # Create the overlay
    coreOverlayDir=$testRootDir/Tests/coreoverlay
    export CORE_ROOT="$coreOverlayDir"
    if [ -e "$coreOverlayDir" ]; then
        rm -f -r "$coreOverlayDir"
    fi
    mkdir "$coreOverlayDir"

	cp -f -v "$coreFxBinDir"/* "$coreOverlayDir/" 2>/dev/null
    cp -f -v "$coreClrBinDir/"* "$coreOverlayDir/" 2>/dev/null
    cp -f -v "$mscorlibDir/mscorlib.dll" "$coreOverlayDir/"
    cp -n -v "$testDependenciesDir"/* "$coreOverlayDir/" 2>/dev/null
    if [ -f "$coreOverlayDir/mscorlib.ni.dll" ]; then
        # Test dependencies come from a Windows build, and mscorlib.ni.dll would be the one from Windows
        rm -f "$coreOverlayDir/mscorlib.ni.dll"
    fi
}

function precompile_overlay_assemblies {

    if [ "$doCrossgen" == "1" ]; then

        local overlayDir=$CORE_ROOT

        filesToPrecompile=$(ls -trh $overlayDir/*.dll)
        for fileToPrecompile in ${filesToPrecompile}
        do
            local filename=${fileToPrecompile}
            # Precompile any assembly except mscorlib since we already have its NI image available.
            if [[ "$filename" != *"mscorlib.dll"* ]]; then
                if [[ "$filename" != *"mscorlib.ni.dll"* ]]; then
                    echo Precompiling $filename
                    $overlayDir/crossgen /Platform_Assemblies_Paths $overlayDir $filename 2>/dev/null
                    local exitCode=$?
                    if [ $exitCode == -2146230517 ]; then
                        echo $filename is not a managed assembly.
                    elif [ $exitCode != 0 ]; then
                        echo Unable to precompile $filename.
                    else
                        echo Successfully precompiled $filename
                    fi
                fi
            fi
        done
    else
        echo Skipping crossgen of FX assemblies.
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
uploadToBenchview=
benchViewOS=
runType=

for i in "$@"
do
    case $i in
        -h|--help)
            print_usage
            exit $EXIT_CODE_SUCCESS
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
		--benchViewOS=*)
            benchViewOS=${i#*=}
            ;;
		--runType=*)
            runType=${i#*=}
            ;;
        --uploadToBenchview)
            uploadToBenchview=TRUE
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

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
if [ -z "$mscorlibDir" ]; then
    mscorlibDir=$coreClrBinDir
fi
if [ -d "$mscorlibDir" ] && [ -d "$mscorlibDir/bin" ]; then
    cp $mscorlibDir/bin/* $mscorlibDir
fi

# Install xunit performance packages
export NUGET_PACKAGES=$testNativeBinDir/../../../../packages
echo "NUGET_PACKAGES = $NUGET_PACKAGES"

pushd $testNativeBinDir/../../../../tests/scripts
$testNativeBinDir/../../../../Tools/dotnetcli/dotnet restore --fallbacksource https://dotnet.myget.org/F/dotnet-buildtools/ --fallbacksource https://dotnet.myget.org/F/dotnet-core/
popd

# Creat coreoverlay dir which contains all dependent binaries
create_core_overlay
precompile_overlay_assemblies
copy_test_native_bin_to_test_root

# Deploy xunit performance packages
cd $CORE_ROOT
echo "CORE_ROOT dir = $CORE_ROOT"

DO_SETUP=TRUE

if [ ${DO_SETUP} == "TRUE" ]; then
cp  $testNativeBinDir/../../../../../packages/Microsoft.DotNet.xunit.performance.runner.cli/1.0.0-alpha-build0040/lib/netstandard1.3/Microsoft.DotNet.xunit.performance.runner.cli.dll .
cp  $testNativeBinDir/../../../../../packages/Microsoft.DotNet.xunit.performance.analysis.cli/1.0.0-alpha-build0040/lib/netstandard1.3/Microsoft.DotNet.xunit.performance.analysis.cli.dll .
cp  $testNativeBinDir/../../../../../packages/Microsoft.DotNet.xunit.performance.run.core/1.0.0-alpha-build0040/lib/dotnet/*.dll .
fi

# Run coreclr performance tests
echo "Test root dir is: $testRootDir"
tests=($(find $testRootDir/JIT/Performance/CodeQuality -name '*.exe') $(find $testRootDir/performance/perflab/PerfLab -name '*.dll'))

echo "current dir is $PWD"
rm measurement.json
for testcase in ${tests[@]}; do

test=$(basename $testcase)
testname=$(basename $testcase .exe)
echo "....Running $testname"
cp $testcase .

chmod u+x ./corerun
echo "./corerun Microsoft.DotNet.xunit.performance.runner.cli.dll $test -runner xunit.console.netcore.exe -runnerhost ./corerun -verbose -runid perf-$testname"
./corerun Microsoft.DotNet.xunit.performance.runner.cli.dll $test -runner xunit.console.netcore.exe -runnerhost ./corerun -verbose -runid perf-$testname
echo "./corerun Microsoft.DotNet.xunit.performance.analysis.cli.dll perf-$testname.xml -xml perf-$testname-summary.xml"
./corerun Microsoft.DotNet.xunit.performance.analysis.cli.dll perf-$testname.xml -xml perf-$testname-summary.xml
if [ "$uploadToBenchview" == "TRUE" ]
    then
	python3.5 ../../../../../tests/scripts/Microsoft.BenchView.JSONFormat/tools/measurement.py xunit perf-$testname.xml --better desc --drop-first-value --append
fi
done
if [ "$uploadToBenchview" == "TRUE" ]
    then
	python3.5 ../../../../../tests/scripts/Microsoft.BenchView.JSONFormat/tools/submission.py measurement.json --build ../../../../../build.json --machine-data ../../../../../machinedata.json --metadata ../../../../../submission-metadata.json --group "CoreCLR" --type "$runType" --config-name "Release" --config Configuration "Release" --config OS "$benchViewOS" --arch "x64" --machinepool "Perfsnake"
	python3.5 ../../../../../tests/scripts/Microsoft.BenchView.JSONFormat/tools/upload.py submission.json --container coreclr
fi
mkdir ../../../../../sandbox
cp *.xml ../../../../../sandbox
