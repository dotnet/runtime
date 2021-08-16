#!/usr/bin/env bash

function print_usage {
    echo ''
    echo 'CoreCLR test runner script.'
    echo ''
    echo 'Typical command line:'
    echo ''
    echo 'src/tests/run.sh <options>'
    echo ''
    echo 'Optional arguments:'
    echo '  -h|--help                        : Show usage information.'
    echo '  -v, --verbose                    : Show output from each test.'
    echo '  <arch>                           : One of x64, x86, arm, arm64, wasm. Defaults to current architecture.'
    echo '  Android                          : Set build OS to Android.'
    echo '  --test-env=<path>                : Script to set environment variables for tests'
    echo '  --testRootDir=<path>             : Root directory of the test build (e.g. runtime/artifacts/tests/windows.x64.Debug).'
    echo '  --disableEventLogging            : Disable the events logged by both VM and Managed Code'
    echo '  --sequential                     : Run tests sequentially (default is to run in parallel).'
    echo '  --runcrossgen2tests              : Runs the ReadyToRun tests compiled with Crossgen2' 
    echo '  --jitstress=<n>                  : Runs the tests with COMPlus_JitStress=n'
    echo '  --jitstressregs=<n>              : Runs the tests with COMPlus_JitStressRegs=n'
    echo '  --jitminopts                     : Runs the tests with COMPlus_JITMinOpts=1'
    echo '  --jitforcerelocs                 : Runs the tests with COMPlus_ForceRelocs=1'
    echo '  --gcname=<n>                     : Runs the tests with COMPlus_GCName=n'
    echo '  --gcstresslevel=<n>              : Runs the tests with COMPlus_GCStress=n'
    echo '    0: None                                1: GC on all allocs and '"'easy'"' places'
    echo '    2: GC on transitions to preemptive GC  4: GC on every allowable JITed instr'
    echo '    8: GC on every allowable NGEN instr   16: GC only on a unique stack trace'
    echo '  --gcsimulator                    : Runs the GCSimulator tests'
    echo '  --long-gc                        : Runs the long GC tests'
    echo '  --useServerGC                    : Enable server GC for this test run'
    echo '  --ilasmroundtrip                 : Runs ilasm round trip on the tests'
    echo '  --link <ILlink>                  : Runs the tests after linking via ILlink'
    echo '  --printLastResultsOnly           : Print the results of the last run'
    echo '  --runincontext                   : Run each tests in an unloadable AssemblyLoadContext'
    echo '  --limitedDumpGeneration          : '
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
        aarch64|arm64)
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

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.
readonly EXIT_CODE_EXCEPTION=1     # Script exited because something exceptional happened (e.g. bad arguments, Ctrl-C interrupt).
readonly EXIT_CODE_TEST_FAILURE=2  # Script completed successfully, but one or more tests failed.

# Argument variables
buildArch=$ARCH
buildOS=
buildConfiguration="Debug"
testRootDir=
testEnv=
gcsimulator=
longgc=
limitedCoreDumps=
((disableEventLogging = 0))
((serverGC = 0))

# Handle arguments
verbose=0
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
        wasm)
            buildArch="wasm"
            ;;
        Android)
            buildOS="Android"
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
        --jitstress=*)
            export COMPlus_JitStress=${i#*=}
            ;;
        --jitstressregs=*)
            export COMPlus_JitStressRegs=${i#*=}
            ;;
        --jitminopts)
            export COMPlus_JITMinOpts=1
            ;;
        --jitforcerelocs)
            export COMPlus_ForceRelocs=1
            ;;
        --link=*)
            export ILLINK=${i#*=}
            export DoLink=true
            ;;
        --ilasmroundtrip)
            ((ilasmroundtrip = 1))
            ;;
        --testRootDir=*)
            testRootDir=${i#*=}
            ;;
        --disableEventLogging)
            ((disableEventLogging = 1))
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
        --test-env=*)
            testEnv=${i#*=}
            ;;            
        --gcstresslevel=*)
            export COMPlus_GCStress=${i#*=}
            ;;            
        --gcname=*)
            export COMPlus_GCName=${i#*=}
            ;;
        --limitedDumpGeneration)
            limitedCoreDumps=ON
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
# Set environment variables affecting tests.
# (These should be run.py arguments.)
################################################################################

if ((disableEventLogging == 0)); then
    export COMPlus_EnableEventLog=1
fi

if ((serverGC != 0)); then
    export COMPlus_gcServer="$serverGC"
fi

################################################################################
# Call run.py to run tests.
################################################################################

runtestPyArguments=("-arch" "${buildArch}" "-build_type" "${buildConfiguration}")
scriptPath="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"
repoRootDir=$scriptPath/../..

echo "Build Architecture            : ${buildArch}"
echo "Build Configuration           : ${buildConfiguration}"

if [ "$buildArch" = "wasm" ]; then
    runtestPyArguments+=("-os" "Browser")
fi

if [ "$buildOS" = "Android" ]; then
    runtestPyArguments+=("-os" "Android")
fi
    
if [ ! -z "$testRootDir" ]; then
    runtestPyArguments+=("-test_location" "$testRootDir")
    echo "Test Location                 : ${testRootDir}"
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

if [ ! -z "$RunCrossGen2" ]; then
    runtestPyArguments+=("--run_crossgen2_tests")
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

# Run the tests using cross platform run.py
echo "$__Python $repoRootDir/src/tests/run.py ${runtestPyArguments[@]}"
$__Python "$repoRootDir/src/tests/run.py" "${runtestPyArguments[@]}"
exit "$?"
