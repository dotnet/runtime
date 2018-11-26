#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'CoreCLR test runner script.'
    echo ''
    echo 'Typical command line:'
    echo ''
    echo 'coreclr/tests/runtest.sh <arch> <configurations>'
    echo ''
    echo 'Optional arguments:'
    echo '  --testRootDir=<path>             : Root directory of the test build (e.g. coreclr/bin/tests/Windows_NT.x64.Debug).'
    echo '  --testNativeBinDir=<path>        : Directory of the native CoreCLR test build (e.g. coreclr/bin/obj/Linux.x64.Debug/tests).'
    echo '  --coreOverlayDir=<path>          : Directory containing core binaries and test dependencies.'
    echo '  --coreClrBinDir=<path>           : Directory of the CoreCLR build (e.g. coreclr/bin/Product/Linux.x64.Debug).'
    echo '  --build-overlay-only             : Build coreoverlay only, and skip running tests.'
    echo '  --generateLayoutOnly             : Build Core_Root only and skip running tests'
    echo '  --generateLayout                 : Force generating layout, even if core_root is passed.'
    echo '  --disableEventLogging            : Disable the events logged by both VM and Managed Code'
    echo '  --sequential                     : Run tests sequentially (default is to run in parallel).'
    echo '  -v, --verbose                    : Show output from each test.'
    echo '  -h|--help                        : Show usage information.'
    echo '  --useServerGC                    : Enable server GC for this test run'
    echo '  --test-env                       : Script to set environment variables for tests'
    echo '  --crossgen                       : Precompiles the framework managed assemblies'
    echo '  --runcrossgentests               : Runs the ready to run tests' 
    echo '  --jitstress=<n>                  : Runs the tests with COMPlus_JitStress=n'
    echo '  --jitstressregs=<n>              : Runs the tests with COMPlus_JitStressRegs=n'
    echo '  --jitminopts                     : Runs the tests with COMPlus_JITMinOpts=1'
    echo '  --jitforcerelocs                 : Runs the tests with COMPlus_ForceRelocs=1'
    echo '  --jitdisasm                      : Runs jit-dasm on the tests'
    echo '  --gcstresslevel=<n>              : Runs the tests with COMPlus_GCStress=n'
    echo '  --gcname=<n>                     : Runs the tests with COMPlus_GCName=n'
    echo '  --ilasmroundtrip                 : Runs ilasm round trip on the tests'
    echo '    0: None                                1: GC on all allocs and '"'easy'"' places'
    echo '    2: GC on transitions to preemptive GC  4: GC on every allowable JITed instr'
    echo '    8: GC on every allowable NGEN instr   16: GC only on a unique stack trace'
    echo '  --long-gc                        : Runs the long GC tests'
    echo '  --gcsimulator                    : Runs the GCSimulator tests'
    echo '  --tieredcompilation              : Runs the tests with COMPlus_TieredCompilation=1'
    echo '  --link <ILlink>                  : Runs the tests after linking via ILlink'
    echo '  --xunitOutputPath=<path>         : Create xUnit XML report at the specifed path (default: <test root>/coreclrtests.xml)'
    echo '  --buildXUnitWrappers             : Force creating the xunit wrappers, this is useful if there have been changes to issues.targets'
    echo '  --printLastResultsOnly           : Print the results of the last run'
    echo ''
    echo 'CoreFX Test Options '
    echo '  --corefxtests                    : Runs CoreFX tests'
    echo '  --corefxtestsall                 : Runs all available CoreFX tests'
    echo '  --corefxtestlist=<path>          : Runs the CoreFX tests specified in the passed list'   
    echo '  --testHostDir=<path>             : Directory containing a built test host including core binaries, test dependencies' 
    echo '                                     and a dotnet executable'
    echo '  --coreclr-src=<path>             : Specify the CoreCLR root directory. Required to build the TestFileSetup tool for CoreFX testing.'
}

function create_testhost
{
    if [ ! -d "$testHostDir" ]; then
        exit_with_error "$errorSource" "Did not find the test host directory: $testHostDir"
    fi

    # Initialize test variables
    local buildToolsDir=$coreClrSrc/Tools
    local dotnetExe=$buildToolsDir/dotnetcli/dotnet
    local coreClrSrcTestDir=$coreClrSrc/tests
    
    if [ -z $coreClrBinDir ]; then
        local coreClrBinDir=${coreClrSrc}/bin
        export __CoreFXTestDir=${coreClrSrc}/bin/tests/CoreFX
    else
        export __CoreFXTestDir=${coreClrBinDir}/tests/CoreFX    
    fi

    local coreFXTestSetupUtilityName=CoreFX.TestUtils.TestFileSetup
    local coreFXTestSetupUtility="${coreClrSrcTestDir}/src/Common/CoreFX/TestFileSetup/${coreFXTestSetupUtilityName}.csproj"
    local coreFXTestSetupUtilityOutputPath=${__CoreFXTestDir}/TestUtilities
    local coreFXTestBinariesOutputPath=${__CoreFXTestDir}/tests_downloaded
    
    if [ -z $CoreFXTestList]; then
        local CoreFXTestList="${coreClrSrcTestDir}/CoreFX/CoreFX.issues.json"
    fi

    case "${OSName}" in
        # Check if we're running under OSX        
        Darwin)
            local coreFXTestRemoteURL=$(<${coreClrSrcTestDir}/CoreFX/CoreFXTestListURL_OSX.txt)
            local coreFXTestExclusionDef=nonosxtests
        ;;        
        # Default to Linux        
        *)
            local coreFXTestRemoteURL=$(<${coreClrSrcTestDir}/CoreFX/CoreFXTestListURL_Linux.txt)
            local coreFXTestExclusionDef=nonlinuxtests
        ;;
    esac

    local coreFXTestExecutable=xunit.console.netcore.exe
    local coreFXLogDir=${coreClrBinDir}/Logs/CoreFX/
    local coreFXTestExecutableArgs="--notrait category=nonnetcoreapptests --notrait category=${coreFXTestExclusionDef} --notrait category=failing --notrait category=IgnoreForCI --notrait category=OuterLoop --notrait Benchmark=true"

    chmod +x ${dotnetExe}
    resetCommandArgs=("msbuild /t:Restore ${coreFXTestSetupUtility}")
    echo "${dotnetExe} $resetCommandArgs"
    "${dotnetExe}" $resetCommandArgs

    buildCommandArgs=("msbuild ${coreFXTestSetupUtility} /p:OutputPath=${coreFXTestSetupUtilityOutputPath} /p:Platform=${_arch} /p:Configuration=Release")
    echo "${dotnetExe} $buildCommandArgs"
    "${dotnetExe}" $buildCommandArgs
    
    if [ "${RunCoreFXTestsAll}" == "1" ]; then
        local coreFXRunCommand=--runAllTests
    else
        local coreFXRunCommand=--runSpecifiedTests
    fi

    local buildTestSetupUtilArgs=("${coreFXTestSetupUtilityOutputPath}/${coreFXTestSetupUtilityName}.dll --clean --outputDirectory ${coreFXTestBinariesOutputPath} --testListJsonPath ${CoreFXTestList} ${coreFXRunCommand} --dotnetPath ${testHostDir}/dotnet --testUrl ${coreFXTestRemoteURL} --executable ${coreFXTestExecutable} --log ${coreFXLogDir} ${coreFXTestExecutableArgs}")
    echo "${dotnetExe} $buildTestSetupUtilArgs"
    "${dotnetExe}" $buildTestSetupUtilArgs

    local exitCode=$?
    if [ $exitCode != 0 ]; then
        echo Running CoreFX tests finished with failures
    else
        echo Running CoreFX tests finished successfully
    fi    
    
    echo Check ${coreFXLogDir} for test run logs

    exit ${exitCode}
}

function set_up_core_dump_generation {
    # We will only enable dump generation here if we're on Mac or Linux
    if [[ ! ( "$(uname -s)" == "Darwin" || "$(uname -s)" == "Linux" ) ]]; then
        return
    fi

    # We won't enable dump generation on OS X/macOS if the machine hasn't been
    # configured with the kern.corefile pattern we expect.
    if [[ ( "$(uname -s)" == "Darwin" && "$(sysctl -n kern.corefile)" != "core.%P" ) ]]; then
        echo "WARNING: Core dump generation not being enabled due to unexpected kern.corefile value."
        return
    fi

    # Allow dump generation
    ulimit -c unlimited

    if [ "$(uname -s)" == "Linux" ]; then
        if [ -e /proc/self/coredump_filter ]; then
            # Include memory in private and shared file-backed mappings in the dump.
            # This ensures that we can see disassembly from our shared libraries when
            # inspecting the contents of the dump. See 'man core' for details.
            echo 0x3F > /proc/self/coredump_filter
        fi
    fi
}

function check_cpu_architecture {
    local CPUName=$(uname -m)
    local __arch=

    case $CPUName in
        i686)
            __arch=x86
            ;;
        x86_64)
            __arch=x64
            ;;
        armv7l)
            __arch=arm
            ;;
        aarch64)
            __arch=arm64
            ;;
        *)
            echo "Unknown CPU $CPUName detected, configuring as if for x64"
            __arch=x64
            ;;
    esac

    echo "$__arch"
}

################################################################################
# Handle Arguments
################################################################################

ARCH=$(check_cpu_architecture)
echo "Running on  CPU- $ARCH"

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.
readonly EXIT_CODE_EXCEPTION=1     # Script exited because something exceptional happened (e.g. bad arguments, Ctrl-C interrupt).
readonly EXIT_CODE_TEST_FAILURE=2  # Script completed successfully, but one or more tests failed.

# Argument variables
buildArch=$ARCH
buildConfiguration="Debug"
testRootDir=
testNativeBinDir=
coreOverlayDir=
coreClrBinDir=
mscorlibDir=
coreFxBinDir=
coreClrObjs=
coreClrSrc=
coverageOutputDir=
testEnv=
playlistFile=
showTime=
noLFConversion=
buildOverlayOnly=
gcsimulator=
longgc=
limitedCoreDumps=
illinker=
((disableEventLogging = 0))
((serverGC = 0))

# Handle arguments
verbose=0
doCrossgen=0
jitdisasm=0
ilasmroundtrip=
buildXUnitWrappers=
printLastResultsOnly=
generateLayoutOnly=
generateLayout=
runSequential=0

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
        x64)
            buildArch="x64"
            ;;
        x86)
            buildArch="x86"
            ;;
        arm)
            buildArch="arm"
            ;;
        arm64)
            buildArch="arm64"
            ;;
        debug|Debug)
            buildConfiguration="Debug"
            ;;
        checked|Checked)
            buildConfiguration="Checked"
            ;;
        release|Release)
            buildConfiguration="Release"
            ;;
        --buildXUnitWrappers)
            buildXUnitWrappers=1
            ;;
        --printLastResultsOnly)
            printLastResultsOnly=1
            ;;
        --crossgen)
            doCrossgen=1
            ;;
        --jitstress=*)
            export COMPlus_JitStress=${i#*=}
            ;;
        --jitstressregs=*)
            export COMPlus_JitStressRegs=${i#*=}
            ;;
        --jitminopts)
            export COMPlus_JITMinOpts=1
            ;;
        --copyNativeTestBin)
            export copyNativeTestBin=1
            ;;
        --jitforcerelocs)
            export COMPlus_ForceRelocs=1
            ;;
        --link=*)
            export ILLINK=${i#*=}
            export DoLink=true
            ;;
        --tieredcompilation)
            export COMPlus_TieredCompilation=1
            ;;
        --jitdisasm)
            jitdisasm=1
            ;;
        --ilasmroundtrip)
            ((ilasmroundtrip = 1))
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
        --runcrossgentests)
            export RunCrossGen=1
            ;;
        --corefxtests)
            export RunCoreFXTests=1
            ;;
        --corefxtestsall)
            export RunCoreFXTests=1
            export RunCoreFXTestsAll=1
            ;;
        --corefxtestlist)
            export RunCoreFXTests=1
            export CoreFXTestList=${i#*=} 
            ;;
        --testHostDir=*)
            export testHostDir=${i#*=}
            ;;
        --sequential)
            runSequential=1
            ;;
        --useServerGC)
            ((serverGC = 1))
            ;;
        --long-gc)
            ((longgc = 1))
            ;;
        --gcsimulator)
            ((gcsimulator = 1))
            ;;
        --playlist=*)
            playlistFile=${i#*=}
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
        --test-env=*)
            testEnv=${i#*=}
            ;;            
        --gcstresslevel=*)
            export COMPlus_GCStress=${i#*=}
            ;;            
        --gcname=*)
            export COMPlus_GCName=${i#*=}
            ;;
        --show-time)
            showTime=ON
            ;;
        --no-lf-conversion)
            noLFConversion=ON
            ;;
        --build-overlay-only)
            buildOverlayOnly=ON
            ;;
        --generateLayoutOnly)
            generateLayoutOnly=1
            ;;
        --generateLayout)
            generateLayout=1
            ;;
        --limitedDumpGeneration)
            limitedCoreDumps=ON
            ;;
        --xunitOutputPath=*)
            xunitOutputPath=${i#*=}
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

################################################################################
# Runtests
################################################################################

if ((disableEventLogging == 0)); then
    export COMPlus_EnableEventLog=1
fi

export COMPlus_gcServer="$serverGC"

################################################################################
# CoreFX
################################################################################

if [ "$RunCoreFXTests" == 1 ];
then 
    if [ -z "$coreClrSrc" ]
    then
        echo "Coreclr src files are required to run CoreFX tests"
        echo "Coreclr src files root path can be passed using '--coreclr-src' argument"
        print_usage
        exit $EXIT_CODE_EXCEPTION
    fi

    if [ -z "$testHostDir" ]; then
        echo "--testHostDir is required to run CoreFX tests"
        print_usage
        exit $EXIT_CODE_EXCEPTION
    fi
    
    if [ ! -f "$testHostDir/dotnet" ]; then
        echo "Executable dotnet not found in $testHostDir"
        exit $EXIT_CODE_EXCEPTION
    fi

    if [ ! -d "$testHostDir" ]; then
        echo "Directory specified by --testHostDir does not exist: $testRootDir"
        exit $EXIT_CODE_EXCEPTION
    fi

    create_testhost
    exit 0
fi

################################################################################
# Runtest.py
################################################################################

runtestPyArguments=("-arch" "${buildArch}" "-build_type" "${buildConfiguration}")
scriptPath="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

if [ -z "$testRootDir" ]; then
    echo "testRootDir and other existing arguments is no longer required. If the "
    echo "default location is incorrect or does not exist, please use "
    echo "--testRootDir to explicitly override the defaults."

    echo ""
fi

echo "Build Architecture            : ${buildArch}"
echo "Build Configuration           : ${buildConfiguration}"
    
if [ ! -z "$testRootDir" ]; then
    runtestPyArguments+=("-test_location" "$testRootDir")
    echo "Test Location                 : ${testRootDir}"
fi

if [ ! -z "$coreClrBinDir" ]; then
    runtestPyArguments+=("-product_location" "$coreClrBinDir")
    echo "Product Location              : ${coreClrBinDir}"
fi

if [ -z "$coreOverlayDir" ]; then
    runtestPyArguments+=("--generate_layout")
else
    runtestPyArguments+=("-core_root" "$coreOverlayDir")
    echo "Core Root Location            : ${coreOverlayDir}"
fi

if [ ! -z "$testNativeBinDir" ]; then
    runtestPyArguments+=("-test_native_bin_location" "$testNativeBinDir")
    echo "Test Native Bin Location      : ${testNativeBinDir}"
fi

if [ ! -z "${testEnv}" ]; then
    runtestPyArguments+=("-test_env" "${testEnv}")
    echo "Test Env                      : ${testEnv}"
fi

echo ""

if [ ! -z "$longgc" ]; then
    echo "Running Long GC tests"
    runtestPyArguments+=("--long_gc")
fi

if [ ! -z "$gcsimulator" ]; then
    echo "Running GC simulator tests"
    runtestPyArguments+=("--gcsimulator")
fi

if [[ ! "$jitdisasm" -eq 0 ]]; then
    echo "Running jit disasm"
    runtestPyArguments+=("--jitdisasm")
fi

if [ ! -z "$ilasmroundtrip" ]; then
    echo "Running Ilasm round trip"
    runtestPyArguments+=("--ilasmroundtrip")
fi

if [ ! -z "$buildXUnitWrappers" ]; then
    runtestPyArguments+=("--build_xunit_test_wrappers")
else
    echo "Skipping xunit wrapper build. If build-test was called on a different"
    echo "host_os or arch the test run will most likely have failures."
fi

if (($verbose!=0)); then
    runtestPyArguments+=("--verbose")
fi

if [ ! -z "$buildOverlayOnly" ] || [ ! -z "$generateLayoutOnly" ]; then
    echo "Will only Generate Core_Root"
    runtestPyArguments+=("--generate_layout_only")
fi

if [ ! -z "$generateLayout" ]; then
    runtestPyArguments+=("--generate_layout")
fi

if [ ! "$runSequential" -eq 0 ]; then
    echo "Run tests sequentially."
    runtestPyArguments+=("--sequential")
fi

if [ ! -z "$printLastResultsOnly" ]; then
    runtestPyArguments+=("--analyze_results_only")
fi

if [ ! -z "$RunCrossGen" ]; then
    runtestPyArguments+=("--run_crossgen_tests")
fi

if (($doCrossgen!=0)); then
    runtestPyArguments+=("--precompile_core_root")
fi

if [ "$limitedCoreDumps" == "ON" ]; then
    runtestPyArguments+=("--limited_core_dumps")
fi

# Default to python3 if it is installed
__Python=python
 if command -v python3 &>/dev/null; then
    __Python=python3
fi

# Run the tests using cross platform runtest.py
echo "python ${scriptPath}/runtest.py ${runtestPyArguments[@]}"
$__Python "${scriptPath}/runtest.py" "${runtestPyArguments[@]}"
exit "$?"
