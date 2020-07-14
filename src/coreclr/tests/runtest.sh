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
    echo '  --testRootDir=<path>             : Root directory of the test build (e.g. runtime/artifacts/tests/Windows_NT.x64.Debug).'
    echo '  --testNativeBinDir=<path>        : Directory of the native CoreCLR test build (e.g. runtime/artifacts/obj/Linux.x64.Debug/tests).'
    echo '  --coreOverlayDir=<path>          : Directory containing core binaries and test dependencies.'
    echo '  --coreClrBinDir=<path>           : Directory of the CoreCLR build (e.g. runtime/artifacts/bin/coreclr/Linux.x64.Debug).'
    echo '  --build-overlay-only             : Build coreoverlay only, and skip running tests.'
    echo '  --disableEventLogging            : Disable the events logged by both VM and Managed Code'
    echo '  --sequential                     : Run tests sequentially (default is to run in parallel).'
    echo '  -v, --verbose                    : Show output from each test.'
    echo '  -h|--help                        : Show usage information.'
    echo '  --useServerGC                    : Enable server GC for this test run'
    echo '  --test-env                       : Script to set environment variables for tests'
    echo '  --crossgen                       : Precompiles the framework managed assemblies'
    echo '  --runcrossgentests               : Runs the ready to run tests' 
    echo '  --runcrossgen2tests              : Runs the ready to run tests compiled with Crossgen2' 
    echo '  --jitstress=<n>                  : Runs the tests with COMPlus_JitStress=n'
    echo '  --jitstressregs=<n>              : Runs the tests with COMPlus_JitStressRegs=n'
    echo '  --jitminopts                     : Runs the tests with COMPlus_JITMinOpts=1'
    echo '  --jitforcerelocs                 : Runs the tests with COMPlus_ForceRelocs=1'
    echo '  --jitdisasm                      : Runs jit-dasm on the tests'
    echo '  --gcstresslevel=<n>              : Runs the tests with COMPlus_GCStress=n'
    echo '    0: None                                1: GC on all allocs and '"'easy'"' places'
    echo '    2: GC on transitions to preemptive GC  4: GC on every allowable JITed instr'
    echo '    8: GC on every allowable NGEN instr   16: GC only on a unique stack trace'
    echo '  --gcname=<n>                     : Runs the tests with COMPlus_GCName=n'
    echo '  --long-gc                        : Runs the long GC tests'
    echo '  --ilasmroundtrip                 : Runs ilasm round trip on the tests'
    echo '  --gcsimulator                    : Runs the GCSimulator tests'
    echo '  --tieredcompilation              : Runs the tests with COMPlus_TieredCompilation=1'
    echo '  --link <ILlink>                  : Runs the tests after linking via ILlink'
    echo '  --xunitOutputPath=<path>         : Create xUnit XML report at the specifed path (default: <test root>/coreclrtests.xml)'
    echo '  --printLastResultsOnly           : Print the results of the last run'
    echo '  --runincontext                   : Run each tests in an unloadable AssemblyLoadContext'
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

    if [[ "$(uname -s)" == "SunOS" ]]; then
        CPUName=$(isainfo -n)
    fi

    case $CPUName in
        i686)
            __arch=x86
            ;;
        amd64|x86_64)
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
coreClrObjs=
coverageOutputDir=
testEnv=
playlistFile=
showTime=
noLFConversion=
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
printLastResultsOnly=
runSequential=0
runincontext=0

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
        --runcrossgen2tests)
            export RunCrossGen2=1
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
        --limitedDumpGeneration)
            limitedCoreDumps=ON
            ;;
        --xunitOutputPath=*)
            xunitOutputPath=${i#*=}
            ;;
        --runincontext)
            runincontext=1
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

if (($verbose!=0)); then
    runtestPyArguments+=("--verbose")
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

if [ ! -z "$RunCrossGen2" ]; then
    runtestPyArguments+=("--run_crossgen2_tests")
fi

if (($doCrossgen!=0)); then
    runtestPyArguments+=("--precompile_core_root")
fi

if [ "$limitedCoreDumps" == "ON" ]; then
    runtestPyArguments+=("--limited_core_dumps")
fi

if [[ ! "$runincontext" -eq 0 ]]; then
    echo "Running in an unloadable AssemblyLoadContext"
    runtestPyArguments+=("--run_in_context")
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
