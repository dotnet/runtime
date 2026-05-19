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
    echo '  wasi                             : Set build OS to WASI.'
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
    echo '  --interpreter                    : Runs the tests with the interpreter enabled'
    echo '  --node                           : Runs the tests with NodeJS (wasm only)'
    echo '  --tree=<path>                    : Only run tests under the specified subtree (e.g. JIT/Regression)'
    echo '  --bxl                           : Run the BuildXL-backed test flow (Linux x64 Checked only)'
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
bxlRun=0
bxlFilter=
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
treeSubtree=

for i in "$@"
do
    if [[ "$__nextTreeArg" == "1" ]]; then
        treeSubtree="$i"
        __nextTreeArg=
        continue
    fi

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
        wasi)
            buildOS="wasi"
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
        --ilasmroundtrip)
            ((ilasmroundtrip = 1))
            ;;
        --testRootDir=*)
            testRootDir=${i#*=}
            ;;
        --bxl)
            bxlRun=1
            ;;
        --bxl-filter=*)
            bxlFilter=${i#*=}
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
        --tree=*|-tree=*)
            treeSubtree=${i#*=}
            ;;
        --tree:*|-tree:*)
            treeSubtree=${i#*:}
            ;;
        --tree|-tree)
            __nextTreeArg=1
            ;;
        --interpreter)
            export RunInterpreter=1
            ;;
        --node)
            export RunWithNodeJS=1
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

# Set default for RunWithNodeJS when using wasm architecture
if [ "$buildArch" = "wasm" ] && [ -z "$RunWithNodeJS" ]; then
    export RunWithNodeJS=1
fi

resolve_bxl_tree_path() {
    local input_path="$1"
    local resolved_path=

    if [[ "$input_path" = /* ]]; then
        resolved_path="$(realpath -m "$input_path")"
    else
        resolved_path="$(realpath -m "$repoRootDir/src/tests/$input_path")"
    fi

    if [[ ! -e "$resolved_path" ]]; then
        echo "BuildXL tree path does not exist: $input_path" >&2
        return $EXIT_CODE_EXCEPTION
    fi

    if [[ "$resolved_path" != "$repoRootDir/src/tests" && "$resolved_path" != "$repoRootDir/src/tests/"* ]]; then
        echo "BuildXL tree paths must stay under $repoRootDir/src/tests: $input_path" >&2
        return $EXIT_CODE_EXCEPTION
    fi

    printf '%s\n' "$resolved_path"
}

run_tests_with_bxl() {
    local resolvedTreePath=
    local bxlCoreRoot="$repoRootDir/artifacts/tests/coreclr/linux.x64.Checked/Tests/Core_Root"
    local exitCode=0

    if [[ "$buildArch" != "x64" ]]; then
        echo "BuildXL test flow currently supports only x64."
        exit $EXIT_CODE_EXCEPTION
    fi

    if [[ "$buildConfiguration" != "Checked" ]]; then
        echo "BuildXL test flow currently supports only Checked runtime configuration."
        exit $EXIT_CODE_EXCEPTION
    fi

    if [[ -n "$buildOS" && "$buildOS" != "linux" ]]; then
        echo "BuildXL test flow currently supports only Linux."
        exit $EXIT_CODE_EXCEPTION
    fi

    if [[ ! -x "$bxlCoreRoot/corerun" || ! -x "$bxlCoreRoot/ilasm" ]]; then
        echo "BuildXL test flow requires a Checked Core_Root at: $bxlCoreRoot" >&2
        echo "Generate it with:" >&2
        echo "    ./build.sh clr+libs+clr.iltools -lc Release -rc Checked" >&2
        echo "    src/tests/build.sh checked x64 generatelayoutonly" >&2
        exit $EXIT_CODE_EXCEPTION
    fi

    if [[ -n "$testRootDir" || -n "$coreRootDir" || -n "$logsDir" || -n "$testEnv" || -n "$longgc" || -n "$gcsimulator" || -n "$ilasmroundtrip" || -n "$printLastResultsOnly" || "$runSequential" -ne 0 || "$verbose" -ne 0 || "$runincontext" -ne 0 || "$tieringtest" -ne 0 || "$nativeaottest" -ne 0 || -n "$RunCrossGen2" || -n "$CrossGen2SynthesizePgo" || -n "$RunInterpreter" || -n "$RunWithNodeJS" || "$limitedCoreDumps" == "ON" ]]; then
        echo "BuildXL test flow currently supports only the standard Linux x64 Checked run plus --tree."
        exit $EXIT_CODE_EXCEPTION
    fi

    if [[ -z "$bxlFilter" && -n "$treeSubtree" ]]; then
        if ! resolvedTreePath="$(resolve_bxl_tree_path "$treeSubtree")"; then
            exit $EXIT_CODE_EXCEPTION
        fi
        bxlFilter="spec='${resolvedTreePath}/*'"
    fi

    echo "Running tests via BuildXL"
    if [[ -n "$bxlFilter" ]]; then
        echo "BuildXL filter                : ${bxlFilter}"
        BXL_FILTER_APPEND="$bxlFilter" bash "$repoRootDir/bxl.sh" test
    else
        bash "$repoRootDir/bxl.sh" test
    fi

    exitCode=$?
    exit "$exitCode"
}

if [[ "$bxlRun" -ne 0 ]]; then
    run_tests_with_bxl
fi

################################################################################
# Call run.py to run tests.
################################################################################

runtestPyArguments=("-arch" "${buildArch}" "-build_type" "${buildConfiguration}")

echo "Build Architecture            : ${buildArch}"
echo "Build Configuration           : ${buildConfiguration}"

if [ "$buildArch" = "wasm" -a -z "$buildOS" ]; then
    buildOS="browser"
fi

if [ -n "$buildOS" ]; then
    runtestPyArguments+=("-os" "$buildOS")
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

if [[ -n "$RunInterpreter" ]]; then
    echo "Running tests with the interpreter"
    runtestPyArguments+=("--interpreter")
fi

if [[ -n "$RunWithNodeJS" ]]; then
    echo "Running tests with NodeJS"
    runtestPyArguments+=("--node")
fi

if [[ -n "$treeSubtree" ]]; then
    echo "Running tests under subtree   : ${treeSubtree}"
    runtestPyArguments+=("--tree" "$treeSubtree")
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
