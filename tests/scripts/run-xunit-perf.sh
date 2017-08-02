#!/usr/bin/env bash

function run_command {
    echo ""
    echo $USER@`hostname` "$PWD"
    echo `date +"[%m/%d/%Y %H:%M:%S]"`" $ $@"
    "$@"
    return $?
}

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
    echo '  --testRootDir=<path>              : Root directory of the test build (e.g. coreclr/bin/tests/Windows_NT.x64.Debug).'
    echo '  --testNativeBinDir=<path>         : Directory of the native CoreCLR test build (e.g. coreclr/bin/obj/Linux.x64.Debug/tests).'
    echo '  (Also required: Either --coreOverlayDir, or all of the switches --coreOverlayDir overrides)'
    echo ''
    echo 'Optional arguments:'
    echo '  --coreOverlayDir=<path>           : Directory containing core binaries and test dependencies. If not specified, the'
    echo '                                      default is testRootDir/Tests/coreoverlay. This switch overrides --coreClrBinDir,'
    echo '                                      --mscorlibDir, and --coreFxBinDir.'
    echo '  --coreClrBinDir=<path>            : Directory of the CoreCLR build (e.g. coreclr/bin/Product/Linux.x64.Debug).'
    echo '  --mscorlibDir=<path>              : Directory containing the built mscorlib.dll. If not specified, it is expected to be'
    echo '                                       in the directory specified by --coreClrBinDir.'
    echo '  --coreFxBinDir="<path>"           : The path to the unpacked runtime folder that is produced as part of a CoreFX build'
    echo '  --generatebenchviewdata           : BenchView tools directory.'
    echo '  --uploadToBenchview               : Specify this flag in order to have the results of the run uploaded to Benchview.'
    echo '                                      This requires that the generatebenchviewdata, os and runtype flags to be set, and'
    echo '                                      also have the BV_UPLOAD_SAS_TOKEN set to a SAS token for the Benchview upload container'
    echo '  --benchViewOS=<os>                : Specify the os that will be used to insert data into Benchview.'
    echo '  --runType=<local|private|rolling> : Specify the runType for Benchview. [Default: local]'
}

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

function exit_with_error {
    local errorSource=$1
    local errorMessage=$2
    local printUsage=$3

    if [ -z "$printUsage" ]; then
        ((printUsage = 0))
    fi

    echo "$errorMessage"
    if ((printUsage != 0)); then
        print_usage
    fi

    echo "Exiting script with error code: $EXIT_CODE_EXCEPTION"
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
        return 0
    fi

    # Check inputs to make sure we have enough information to create the core
    # layout. $testRootDir/Tests/Core_Root should already exist and contain test
    # dependencies that are not built.
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
    if [ -z "$coreFxBinDir" ]; then
        exit_with_error "$errorSource" "One of --coreOverlayDir or --coreFxBinDir must be specified." "$printUsage"
    fi

    # Create the overlay
    coreOverlayDir=$testRootDir/Tests/coreoverlay
    export CORE_ROOT="$coreOverlayDir"
    if [ -e "$coreOverlayDir" ]; then
        rm -rf "$coreOverlayDir" || exit 1
    fi

    mkdir "$coreOverlayDir"

    cp -f -v "$coreFxBinDir/"* "$coreOverlayDir/"               || exit 2
    cp -f -p -v "$coreClrBinDir/"* "$coreOverlayDir/"           # || exit 3
    if [ -d "$mscorlibDir/bin" ]; then
        cp -f -v "$mscorlibDir/bin/"* "$coreOverlayDir/"        || exit 4
    fi
    cp -f -v "$testDependenciesDir/"xunit* "$coreOverlayDir/"   || exit 5
    cp -n -v "$testDependenciesDir/"* "$coreOverlayDir/"        # || exit 6
    if [ -f "$coreOverlayDir/mscorlib.ni.dll" ]; then
        # Test dependencies come from a Windows build, and mscorlib.ni.dll would be the one from Windows
        rm -f "$coreOverlayDir/mscorlib.ni.dll"                 || exit 7
    fi
    if [ -f "$coreOverlayDir/System.Private.CoreLib.ni.dll" ]; then
        # Test dependencies come from a Windows build, and System.Private.CoreLib.ni.dll would be the one from Windows
        rm -f "$coreOverlayDir/System.Private.CoreLib.ni.dll"   || exit 8
    fi

    copy_test_native_bin_to_test_root                           || exit 9

    return 0
}

function precompile_overlay_assemblies {

    if [ "$doCrossgen" == "1" ]; then

        local overlayDir=$CORE_ROOT

        filesToPrecompile=$(ls -trh $overlayDir/*.dll)
        for fileToPrecompile in ${filesToPrecompile}
        do
            local filename=${fileToPrecompile}
            echo "Precompiling $filename"
            $overlayDir/crossgen /Platform_Assemblies_Paths $overlayDir $filename 2>/dev/null
            local exitCode=$?
            if [ $exitCode == -2146230517 ]; then
                echo "$filename is not a managed assembly."
            elif [ $exitCode != 0 ]; then
                echo "Unable to precompile $filename."
            else
                echo "Successfully precompiled $filename"
            fi
        done
    else
        echo "Skipping crossgen of FX assemblies."
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
benchViewOS=`lsb_release -i -s``lsb_release -r -s`
runType=local
BENCHVIEW_TOOLS_PATH=
benchViewGroup=CoreCLR
perfCollection=
collectionflags=stopwatch
hasWarmupRun=--drop-first-value
stabilityPrefix=

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
        --collectionflags=*)
            collectionflags=${i#*=}
            ;;
        --generatebenchviewdata=*)
            BENCHVIEW_TOOLS_PATH=${i#*=}
            ;;
        --stabilityPrefix=*)
            stabilityPrefix=${i#*=}
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
if [ ! -z "$BENCHVIEW_TOOLS_PATH" ] && { [ ! -d "$BENCHVIEW_TOOLS_PATH" ]; }; then
    echo BenchView path: "$BENCHVIEW_TOOLS_PATH" was specified, but it does not exist.
    exit $EXIT_CODE_EXCEPTION
fi
if [ "$collectionflags" == "stopwatch" ]; then
    perfCollection=Off
else
    perfCollection=On
fi

# Install xunit performance packages
CORECLR_REPO=$testNativeBinDir/../../../..
DOTNETCLI_PATH=$CORECLR_REPO/Tools/dotnetcli

export NUGET_PACKAGES=$CORECLR_REPO/packages

# Creat coreoverlay dir which contains all dependent binaries
create_core_overlay                 || { echo "Creating core overlay failed."; exit 1; }
precompile_overlay_assemblies       || { echo "Precompiling overlay assemblies failed."; exit 1; }

# Deploy xunit performance packages
cd $CORE_ROOT

DO_SETUP=TRUE
if [ ${DO_SETUP} == "TRUE" ]; then
    $DOTNETCLI_PATH/dotnet restore $CORECLR_REPO/tests/src/Common/PerfHarness/PerfHarness.csproj                                    || { echo "dotnet restore failed."; exit 1; }
    $DOTNETCLI_PATH/dotnet publish $CORECLR_REPO/tests/src/Common/PerfHarness/PerfHarness.csproj -c Release -o "$coreOverlayDir"    || { echo "dotnet publish failed."; exit 1; }
fi

# Run coreclr performance tests
echo "Test root dir: $testRootDir"
tests=($(find $testRootDir/JIT/Performance/CodeQuality -name '*.exe') $(find $testRootDir/performance/perflab/PerfLab -name '*.dll'))

if [ -f measurement.json ]; then
    rm measurement.json || exit $EXIT_CODE_EXCEPTION;
fi

for testcase in ${tests[@]}; do
    directory=$(dirname "$testcase")
    filename=$(basename "$testcase")
    filename="${filename%.*}"

    test=$(basename $testcase)
    testname=$(basename $testcase .exe)

    cp $testcase .                    || exit 1
    if [ stat -t "$directory/$filename"*.txt 1>/dev/null 2>&1 ]; then
        cp "$directory/$filename"*.txt .  || exit 1
    fi

    # TODO: Do we need this here.
    chmod u+x ./corerun

    echo ""
    echo "----------"
    echo "  Running $testname"
    echo "----------"
    run_command $stabilityPrefix ./corerun PerfHarness.dll $test --perf:runid Perf --perf:collect $collectionflags 1>"Perf-$filename.log" 2>&1 || exit 1
    if [ -d "$BENCHVIEW_TOOLS_PATH" ]; then
        run_command python3.5 "$BENCHVIEW_TOOLS_PATH/measurement.py" xunit "Perf-$filename.xml" --better desc $hasWarmupRun --append || {
            echo [ERROR] Failed to generate BenchView data;
            exit 1;
        }
    fi

    # Rename file to be archived by Jenkins.
    mv -f "Perf-$filename.log" "$CORECLR_REPO/Perf-$filename-$perfCollection.log" || {
        echo [ERROR] Failed to move "Perf-$filename.log" to "$CORECLR_REPO".
        exit 1;
    }
    mv -f "Perf-$filename.xml" "$CORECLR_REPO/Perf-$filename-$perfCollection.xml" || {
        echo [ERROR] Failed to move "Perf-$filename.xml" to "$CORECLR_REPO".
        exit 1;
    }
done

if [ -d "$BENCHVIEW_TOOLS_PATH" ]; then
    args=measurement.json
    args+=" --build ../../../../../build.json"
    args+=" --machine-data ../../../../../machinedata.json"
    args+=" --metadata ../../../../../submission-metadata.json"
    args+=" --group $benchViewGroup"
    args+=" --type $runType"
    args+=" --config-name Release"
    args+=" --config Configuration Release"
    args+=" --config OS $benchViewOS"
    args+=" --config Profile $perfCollection"
    args+=" --architecture x64"
    args+=" --machinepool Perfsnake"
    run_command python3.5 "$BENCHVIEW_TOOLS_PATH/submission.py" $args || {
        echo [ERROR] Failed to generate BenchView submission data;
        exit 1;
    }
fi

if [ -d "$BENCHVIEW_TOOLS_PATH" ] && { [ "$uploadToBenchview" == "TRUE" ]; }; then
    run_command python3.5 "$BENCHVIEW_TOOLS_PATH/upload.py" submission.json --container coreclr
fi
