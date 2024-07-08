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
    echo '  <arch>                           : One of x64, x86, arm, arm64, loongarch64, riscv64, wasm. Defaults to current architecture.'
    echo '  <build configuration>            : One of debug, checked, release. Defaults to debug.'
    echo '  android                          : Set build OS to Android.'
    echo '  --test-env=<path>                : Script to set environment variables for tests'
    echo '  --testRootDir=<path>             : Root directory of the test build (e.g. runtime/artifacts/tests/windows.x64.Debug).'
    echo '  --coreRootDir=<path>             : Directory to the CORE_ROOT location.'
    echo '  --enableEventLogging             : Enable event logging through LTTNG.'
    echo '  --sequential                     : Run tests sequentially (default is to run in parallel).'
    echo '  --runcrossgen2tests              : Runs the ReadyToRun tests compiled with Crossgen2'
    echo '  --synthesizepgo                  : Runs the tests allowing crossgen2 to synthesize PGO data'
    echo '  --jitstress=<n>                  : Runs the tests with DOTNET_JitStress=n'
    echo '  --jitstressregs=<n>              : Runs the tests with DOTNET_JitStressRegs=n'
    echo '  --jitminopts                     : Runs the tests with DOTNET_JITMinOpts=1'
    echo '  --jitforcerelocs                 : Runs the tests with DOTNET_ForceRelocs=1'
    echo '  --gcname=<n>                     : Runs the tests with DOTNET_GCName=n'
    echo '  --gcstresslevel=<n>              : Runs the tests with DOTNET_GCStress=n'
    echo '    0: None                                1: GC on all allocs and '"'easy'"' places'
    echo '    2: GC on transitions to preemptive GC  4: GC on every allowable JITed instr'
    echo '    8: GC on every allowable NGEN instr   16: GC only on a unique stack trace'
    echo '  --gcsimulator                    : Runs the GCSimulator tests'
    echo '  --long-gc                        : Runs the long GC tests'
    echo '  --useServerGC                    : Enable server GC for this test run'
    echo '  --ilasmroundtrip                 : Runs ilasm round trip on the tests'
    echo '  --link=<ILlink>                  : Runs the tests after linking via ILlink'
    echo '  --printLastResultsOnly           : Print the results of the last run'
    echo '  --logsDir=<path>                 : Specify the logs directory (default: artifacts/log)'
    echo '  --runincontext                   : Run each tests in an unloadable AssemblyLoadContext'
    echo '  --tieringtest                    : Run each test to encourage tier1 rejitting'
    echo '  --runnativeaottests              : Run NativeAOT compiled tests'
    echo '  --limitedDumpGeneration          : '
}

# Exit code constants
readonly EXIT_CODE_SUCCESS=0       # Script ran normally.
readonly EXIT_CODE_EXCEPTION=1     # Script exited because something exceptional happened (e.g. bad arguments, Ctrl-C interrupt).
readonly EXIT_CODE_TEST_FAILURE=2  # Script completed successfully, but one or more tests failed.

scriptPath="$(cd "$(dirname "$BASH_SOURCE[0]")"; pwd -P)"
repoRootDir="$(cd "$scriptPath"/../..; pwd -P)"
source "$repoRootDir/eng/common/native/init-os-and-arch.sh"

# Argument variables
buildArch="$arch"
buildOS=
buildConfiguration="Debug"
testRootDir=
coreRootDir=
logsDir=
testEnv=
gcsimulator=
longgc=
limitedCoreDumps=
verbose=0
ilasmroundtrip=
printLastResultsOnly=
runSequential=0
runincontext=0
tieringtest=0
nativeaottest=0

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
        loongarch64)
            buildArch="loongarch64"
            ;;
        riscv64)
            buildArch="riscv64"
            ;;
        wasm)
            buildArch="wasm"
            ;;
        android)
            buildOS="android"
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
            export DOTNET_JitStress=${i#*=}
            ;;
        --jitstressregs=*)
            export DOTNET_JitStressRegs=${i#*=}
            ;;
        --jitminopts)
            export DOTNET_JITMinOpts=1
            ;;
        --jitforcerelocs)
            export DOTNET_ForceRelocs=1
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
        --coreRootDir=*)
            coreRootDir=${i#*=}
            ;;
        --logsDir=*)
            logsDir=${i#*=}
            ;;
        --enableEventLogging)
            export DOTNET_EnableEventLog=1
            ;;
        --runcrossgen2tests)
            export RunCrossGen2=1
            ;;
        --synthesizepgo)
            export CrossGen2SynthesizePgo=1
            ;;
        --sequential)
            runSequential=1
            ;;
        --useServerGC)
            export DOTNET_gcServer=1
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
            export DOTNET_GCStress=${i#*=}
            ;;
        --gcname=*)
            export DOTNET_GCName=${i#*=}
            ;;
        --limitedDumpGeneration)
            limitedCoreDumps=ON
            ;;
        --runincontext)
            runincontext=1
            ;;
        --tieringtest)
            tieringtest=1
            ;;
        --runnativeaottests)
            nativeaottest=1
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

################################################################################
# Call run.py to run tests.
################################################################################

runtestPyArguments=("-arch" "${buildArch}" "-build_type" "${buildConfiguration}")

echo "Build Architecture            : ${buildArch}"
echo "Build Configuration           : ${buildConfiguration}"

if [ "$buildArch" = "wasm" ]; then
    runtestPyArguments+=("-os" "browser")
fi

if [ "$buildOS" = "android" ]; then
    runtestPyArguments+=("-os" "android")
fi

if [[ -n "$testRootDir" ]]; then
    runtestPyArguments+=("-test_location" "$testRootDir")
    echo "Test Location                 : ${testRootDir}"
fi

if [[ -n "$coreRootDir" ]]; then
    runtestPyArguments+=("-core_root" "$coreRootDir")
    echo "CORE_ROOT                     : ${coreRootDir}"
fi

if [[ -n "$logsDir" ]]; then
    runtestPyArguments+=("-logs_dir" "$logsDir")
    echo "Logs directory                : ${logsDir}"
fi

if [[ -n "${testEnv}" ]]; then
    runtestPyArguments+=("-test_env" "${testEnv}")
    echo "Test Env                      : ${testEnv}"
fi

echo ""

if [[ -n "$longgc" ]]; then
    echo "Running Long GC tests"
    runtestPyArguments+=("--long_gc")
fi

if [[ -n "$gcsimulator" ]]; then
    echo "Running GC simulator tests"
    runtestPyArguments+=("--gcsimulator")
fi

if [[ -n "$ilasmroundtrip" ]]; then
    echo "Running Ilasm round trip"
    runtestPyArguments+=("--ilasmroundtrip")
fi

if (($verbose!=0)); then
    runtestPyArguments+=("--verbose")
fi

if [ "$runSequential" -ne 0 ]; then
    echo "Run tests sequentially."
    runtestPyArguments+=("--sequential")
fi

if [[ -n "$printLastResultsOnly" ]]; then
    runtestPyArguments+=("--analyze_results_only")
fi

if [[ -n "$RunCrossGen2" ]]; then
    runtestPyArguments+=("--run_crossgen2_tests")
fi

if [[ -n "$CrossGen2SynthesizePgo" ]]; then
    runtestPyArguments+=("--synthesize_pgo")
fi

if [[ "$limitedCoreDumps" == "ON" ]]; then
    runtestPyArguments+=("--limited_core_dumps")
fi

if [[ "$runincontext" -ne 0 ]]; then
    echo "Running in an unloadable AssemblyLoadContext"
    runtestPyArguments+=("--run_in_context")
fi

if [[ "$tieringtest" -ne 0 ]]; then
    echo "Running to encourage tier1 rejitting"
    runtestPyArguments+=("--tieringtest")
fi

if [[ "$nativeaottest" -ne 0 ]]; then
    echo "Running NativeAOT compiled tests"
    runtestPyArguments+=("--run_nativeaot_tests")
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
