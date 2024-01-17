#!/usr/bin/env bash

# Also reset/set below
set -x

source_directory=$BUILD_SOURCESDIRECTORY
core_root_directory=
baseline_core_root_directory=
architecture=x64
framework=
compilation_mode=tiered
repository=$BUILD_REPOSITORY_NAME
branch=$BUILD_SOURCEBRANCH
commit_sha=$BUILD_SOURCEVERSION
build_number=$BUILD_BUILDNUMBER
internal=false
compare=false
mono_dotnet=
kind="micro"
llvm=false
monointerpreter=false
monoaot=false
monoaot_path=
run_categories="Libraries Runtime"
csproj="src\benchmarks\micro\MicroBenchmarks.csproj"
configurations="CompilationMode=$compilation_mode RunKind=$kind"
perf_fork=""
perf_fork_branch="main"
run_from_perf_repo=false
use_core_run=true
use_baseline_core_run=true
using_mono=false
wasm_bundle_directory=
using_wasm=false
use_latest_dotnet=false
logical_machine=
javascript_engine="v8"
javascript_engine_path=""
iosmono=false
iosnativeaot=false
runtimetype=""
iosllvmbuild=""
iosstripsymbols=""
hybridglobalization=""
maui_version=""
use_local_commit_time=false
only_sanity=false
dotnet_versions=""
v8_version=""

while (($# > 0)); do
  lowerI="$(echo $1 | tr "[:upper:]" "[:lower:]")"
  case $lowerI in
    --sourcedirectory)
      source_directory=$2
      shift 2
      ;;
    --corerootdirectory)
      core_root_directory=$2
      shift 2
      ;;
    --baselinecorerootdirectory)
      baseline_core_root_directory=$2
      shift 2
      ;;
    --architecture)
      architecture=$2
      shift 2
      ;;
    --framework)
      framework=$2
      shift 2
      ;;
    --compilationmode)
      compilation_mode=$2
      shift 2
      ;;
    --logicalmachine)
      logical_machine=$2
      shift 2
      ;;
    --repository)
      repository=$2
      shift 2
      ;;
    --branch)
      branch=$2
      shift 2
      ;;
    --commitsha)
      commit_sha=$2
      shift 2
      ;;
    --buildnumber)
      build_number=$2
      shift 2
      ;;
    --javascriptengine)
      javascript_engine=$2
      shift 2
      ;;
    --javascriptenginepath)
      javascript_engine_path=$2
      shift 2
      ;;
    --kind)
      kind=$2
      configurations="CompilationMode=$compilation_mode RunKind=$kind"
      shift 2
      ;;
    --runcategories)
      run_categories=$2
      shift 2
      ;;
    --csproj)
      csproj=$2
      shift 2
      ;;
    --internal)
      internal=true
      shift 1
      ;;
    --alpine)
      alpine=true
      shift 1
      ;;
    --llvm)
      llvm=true
      shift 1
      ;;
    --monointerpreter)
      monointerpreter=true
      shift 1
      ;;
    --monoaot)
      monoaot=true
      monoaot_path=$2
      shift 2
      ;;
    --monodotnet)
      mono_dotnet=$2
      shift 2
      ;;
    --wasmbundle)
      wasm_bundle_directory=$2
      shift 2
      ;;
    --wasmaot)
      wasmaot=true
      shift 1
      ;;
    --nodynamicpgo)
      nodynamicpgo=true
      shift 1
      ;;
    --physicalpromotion)
      physicalpromotion=true
      shift 1
      ;;
    --nor2r)
      nor2r=true
      shift 1
      ;;
    --experimentname)
      experimentname=$2
      shift 2
      ;;
    --compare)
      compare=true
      shift 1
      ;;
    --configurations)
      configurations=$2
      shift 2
      ;;
    --latestdotnet)
      use_latest_dotnet=true
      shift 1
      ;;
    --dotnetversions)
      dotnet_versions="$2"
      shift 2
      ;;
    --iosmono)
      iosmono=true
      shift 1
      ;;
    --iosnativeaot)
      iosnativeaot=true
      shift 1
      ;;
    --iosllvmbuild)
      iosllvmbuild=$2
      shift 2
      ;;
    --iosstripsymbols)
      iosstripsymbols=$2
      shift 2
      ;;
    --hybridglobalization)
      hybridglobalization=$2
      shift 2
      ;;
    --mauiversion)
      maui_version=$2
      shift 2
      ;;
    --uselocalcommittime)
      use_local_commit_time=true
      shift 1
      ;;
    --perffork)
      perf_fork=$2
      shift 2
      ;;
    --perfforkbranch)
      perf_fork_branch=$2
      shift 2
      ;;
    --only-sanity)
      only_sanity=true
      shift 1
      ;;
    *)
      echo "Common settings:"
      echo "  --corerootdirectory <value>    Directory where Core_Root exists, if running perf testing with --corerun"
      echo "  --architecture <value>         Architecture of the testing being run"
      echo "  --configurations <value>       List of key=value pairs that will be passed to perf testing infrastructure."
      echo "                                 ex: --configurations \"CompilationMode=Tiered OptimzationLevel=PGO\""
      echo "  --help                         Print help and exit"
      echo ""
      echo "Advanced settings:"
      echo "  --framework <value>            The framework to run, if not running in master"
      echo "  --compilationmode <value>      The compilation mode if not passing --configurations"
      echo "  --sourcedirectory <value>      The directory of the sources. Defaults to env:BUILD_SOURCESDIRECTORY"
      echo "  --repository <value>           The name of the repository in the <owner>/<repository name> format. Defaults to env:BUILD_REPOSITORY_NAME"
      echo "  --branch <value>               The name of the branch. Defaults to env:BUILD_SOURCEBRANCH"
      echo "  --commitsha <value>            The commit sha1 to run against. Defaults to env:BUILD_SOURCEVERSION"
      echo "  --buildnumber <value>          The build number currently running. Defaults to env:BUILD_BUILDNUMBER"
      echo "  --csproj                       The relative path to the benchmark csproj whose tests should be run. Defaults to src\benchmarks\micro\MicroBenchmarks.csproj"
      echo "  --kind <value>                 Related to csproj. The kind of benchmarks that should be run. Defaults to micro"
      echo "  --runcategories <value>        Related to csproj. Categories of benchmarks to run. Defaults to \"coreclr corefx\""
      echo "  --internal                     If the benchmarks are running as an official job."
      echo "  --monodotnet                   Pass the path to the mono dotnet for mono performance testing."
      echo "  --wasmbundle                   Path to the wasm bundle containing the dotnet, and data needed for helix payload"
      echo "  --wasmaot                      Indicate wasm aot"
      echo "  --latestdotnet                 --dotnet-versions will not be specified. --dotnet-versions defaults to LKG version in global.json "
      echo "  --dotnetversions               Passed as '--dotnet-versions <value>' to the setup script"
      echo "  --alpine                       Set for runs on Alpine"
      echo "  --llvm                         Set LLVM for Mono runs"
      echo "  --iosmono                      Set for ios Mono/Maui runs"
      echo "  --iosnativeaot                 Set for ios Native AOT runs"
      echo "  --iosllvmbuild                 Set LLVM for iOS Mono/Maui runs"
      echo "  --iosstripsymbols              Set STRIP_DEBUG_SYMBOLS for iOS Mono/Maui runs"
      echo "  --hybridglobalization          Set hybrid globalization for iOS Mono/Maui/Wasm runs"
      echo "  --mauiversion                  Set the maui version for Mono/Maui runs"
      echo "  --uselocalcommittime           Pass local runtime commit time to the setup script"
      echo "  --nodynamicpgo                 Set for No dynamic PGO runs"
      echo "  --physicalpromotion            Set for runs with physical promotion"
      echo "  --nor2r                        Set for No R2R runs"
      echo "  --experimentname <value>       Set Experiment Name"
      echo ""
      exit 1
      ;;
  esac
done

if [[ "$repository" == "dotnet/performance" || "$repository" == "dotnet-performance" ]]; then
    run_from_perf_repo=true
fi

if [ -z "$configurations" ]; then
    configurations="CompilationMode=$compilation_mode"
fi

if [ -z "$core_root_directory" ]; then
    use_core_run=false
fi

if [ -z "$baseline_core_root_directory" ]; then
    use_baseline_core_run=false
fi

payload_directory=$source_directory/Payload
performance_directory=$payload_directory/performance
benchmark_directory=$payload_directory/BenchmarkDotNet
workitem_directory=$source_directory/workitem
extra_benchmark_dotnet_arguments="--iterationCount 1 --warmupCount 0 --invocationCount 1 --unrollFactor 1 --strategy ColdStart --stopOnFirstError true"
perflab_arguments=
queue=Ubuntu.2204.Amd64.Open
creator=$BUILD_DEFINITIONNAME
helix_source_prefix="pr"

if [[ "$internal" == true ]]; then
    perflab_arguments="--upload-to-perflab-container"
    helix_source_prefix="official"
    creator=
    extra_benchmark_dotnet_arguments=

    if [[ "$logical_machine" == "perfiphone12mini" ]]; then
        queue=OSX.13.Amd64.Iphone.Perf
    elif [[ "$logical_machine" == "perfampere" ]]; then
        queue=Ubuntu.2204.Arm64.Perf
    elif [[ "$logical_machine" == "cloudvm" ]]; then
        queue=Ubuntu.2204.Amd64
    elif [[ "$architecture" == "arm64" ]]; then
        queue=Ubuntu.1804.Arm64.Perf
    else
        if [[ "$logical_machine" == "perfowl" ]]; then
            queue=Ubuntu.2204.Amd64.Owl.Perf
        elif [[ "$logical_machine" == "perftiger_crossgen" ]]; then
            queue=Ubuntu.1804.Amd64.Tiger.Perf
        else
            queue=Ubuntu.2204.Amd64.Tiger.Perf
        fi
    fi

    if [[ "$alpine" == "true" ]]; then
        queue=alpine.amd64.tiger.perf
    fi
else
    if [[ "$architecture" == "arm64" ]]; then
        queue=ubuntu.1804.armarch.open
    else
        queue=Ubuntu.2204.Amd64.Open
    fi

    if [[ "$alpine" == "true" ]]; then
        queue=alpine.amd64.tiger.perf
    fi
fi

if [[ -n "$mono_dotnet" && "$monointerpreter" == "false" ]]; then
    configurations="$configurations LLVM=$llvm MonoInterpreter=$monointerpreter MonoAOT=$monoaot"
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --category-exclusion-filter NoMono"
fi

_BuildConfig="$architecture.$kind.$framework"

if [[ -n "$wasm_bundle_directory" ]]; then
    if [[ "$wasmaot" == "true" ]]; then
        configurations="CompilationMode=wasm AOT=true RunKind=$kind"
        _BuildConfig="wasmaot.$_BuildConfig"
    else
        configurations="CompilationMode=wasm RunKind=$kind"
        _BuildConfig="wasm.$_BuildConfig"
    fi
    if [[ "$javascript_engine" == "javascriptcore" ]]; then
      configurations="$configurations JSEngine=javascriptcore"
    fi
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --category-exclusion-filter NoInterpreter NoWASM NoMono"
fi

if [[ -n "$mono_dotnet" && "$monointerpreter" == "true" ]]; then
    configurations="$configurations LLVM=$llvm MonoInterpreter=$monointerpreter MonoAOT=$monoaot"
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --category-exclusion-filter NoInterpreter NoMono"
fi

if [[ "$monoaot" == "true" ]]; then
    configurations="$configurations LLVM=$llvm MonoInterpreter=$monointerpreter MonoAOT=$monoaot"
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --category-exclusion-filter NoAOT NoWASM"
fi

if [[ "$iosmono" == "true" ]]; then
    runtimetype="Mono"
    configurations="$configurations iOSLlvmBuild=$iosllvmbuild iOSStripSymbols=$iosstripsymbols RuntimeType=$runtimetype"
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments"
fi

if [[ "$iosnativeaot" == "true" ]]; then
    runtimetype="NativeAOT"
    configurations="$configurations iOSStripSymbols=$iosstripsymbols RuntimeType=$runtimetype"
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments"
fi

if [[ "$nodynamicpgo" == "true" ]]; then
    configurations="$configurations PGOType=nodynamicpgo"
fi

if [[ "$physicalpromotion" == "true" ]]; then
    configurations="$configurations PhysicalPromotionType=physicalpromotion"
fi

if [[ "$nor2r" == "true" ]]; then
    configurations="$configurations R2RType=nor2r"
fi

if [[ ! -z "$experimentname" ]]; then
    configurations="$configurations ExperimentName=$experimentname"
    if [[ "$experimentname" == "memoryRandomization" ]]; then
        extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --memoryRandomization true"
    fi
fi

if [[ "$(echo "$hybridglobalization" | tr '[:upper:]' '[:lower:]')" == "true" ]]; then # convert to lowercase to test
    configurations="$configurations HybridGlobalization=True" # Force True for consistency
fi



cleaned_branch_name="main"
if [[ $branch == *"refs/heads/release"* ]]; then
    cleaned_branch_name=${branch/refs\/heads\//}
fi
common_setup_arguments="--channel $cleaned_branch_name --queue $queue --build-number $build_number --build-configs $configurations --architecture $architecture"
setup_arguments="--repository https://github.com/$repository --branch $branch --get-perf-hash --commit-sha $commit_sha $common_setup_arguments"

if [[ "$internal" != true ]]; then
    setup_arguments="$setup_arguments --not-in-lab"
fi

if [[ "$use_local_commit_time" == true ]]; then
    local_commit_time=$(git show -s --format=%ci $commit_sha)
    setup_arguments="$setup_arguments --commit-time \"$local_commit_time\""
fi

if [[ "$run_from_perf_repo" == true ]]; then
    payload_directory=
    workitem_directory=$source_directory
    performance_directory=$workitem_directory
    setup_arguments="--perf-hash $commit_sha $common_setup_arguments"
else
    if [[ -n "$perf_fork" ]]; then
        git clone --branch $perf_fork_branch --depth 1 --quiet $perf_fork $performance_directory
    else
        git clone --branch main --depth 1 --quiet https://github.com/dotnet/performance.git $performance_directory
    fi
    # uncomment to use BenchmarkDotNet sources instead of nuget packages
    # git clone https://github.com/dotnet/BenchmarkDotNet.git $benchmark_directory

    (cd $performance_directory; git show -s HEAD)

    docs_directory=$performance_directory/docs
    mv $docs_directory $workitem_directory
fi

if [[ -n "$maui_version" ]]; then
    setup_arguments="$setup_arguments --maui-version $maui_version"
fi

if [[ -n "$wasm_bundle_directory" ]]; then
    using_wasm=true
    wasm_bundle_directory_path=$payload_directory
    mv $wasm_bundle_directory/* $wasm_bundle_directory_path
    wasm_args="--expose_wasm"
    if [ "$javascript_engine" == "v8" ]; then
        # for es6 module support
        wasm_args="$wasm_args --module"

        # get required version
        if [[ -z "$v8_version" ]]; then
            v8_version=`grep linux_V8Version $source_directory/eng/testing/ChromeVersions.props | sed -e 's,.*>\([^\<]*\)<.*,\1,g' | cut -d. -f 1-3`
            echo "V8 version: $v8_version"
        fi
        if [[ -z "$javascript_engine_path" ]]; then
            javascript_engine_path="/home/helixbot/.jsvu/bin/v8-${v8_version}"
        fi
    fi

    if [[ -z "$javascript_engine_path" ]]; then
        javascript_engine_path="/home/helixbot/.jsvu/bin/$javascript_engine"
    fi

    # Workaround: escaping the quotes around `--wasmArgs=..` so they get retained for the actual command line
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --wasmEngine $javascript_engine_path \\\"--wasmArgs=$wasm_args\\\" --cli \$HELIX_CORRELATION_PAYLOAD/dotnet/dotnet --wasmDataDir \$HELIX_CORRELATION_PAYLOAD/wasm-data"
    if [[ "$wasmaot" == "true" ]]; then
        extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --aotcompilermode wasm --buildTimeout 3600"
    fi
    setup_arguments="$setup_arguments --dotnet-path $wasm_bundle_directory_path/dotnet"
fi

if [[ -n "$mono_dotnet" && "$monoaot" == "false" ]]; then
    using_mono=true
    mono_dotnet_path=$payload_directory/dotnet-mono
    mv $mono_dotnet $mono_dotnet_path
fi

if [[ -n "$dotnet_versions" ]]; then
    setup_arguments="$setup_arguments --dotnet-versions $dotnet_versions"
fi

if [[ "$nodynamicpgo" == "true" ]]; then
    setup_arguments="$setup_arguments --no-dynamic-pgo"
fi

if [[ "$physicalpromotion" == "true" ]]; then
    setup_arguments="$setup_arguments --physical-promotion"
fi

if [[ "$nor2r" == "true" ]]; then
    setup_arguments="$setup_arguments --no-r2r"
fi

if [[ ! -z "$experimentname" ]]; then
    setup_arguments="$setup_arguments --experiment-name '$experimentname'"
fi

if [[ "$monoaot" == "true" ]]; then
    monoaot_dotnet_path=$payload_directory/monoaot
    mv $monoaot_path $monoaot_dotnet_path
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --runtimes monoaotllvm --aotcompilerpath \$HELIX_CORRELATION_PAYLOAD/monoaot/mono-aot-cross --customruntimepack \$HELIX_CORRELATION_PAYLOAD/monoaot/pack --aotcompilermode llvm"
fi

extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --logBuildOutput --generateBinLog"

if [[ "$use_core_run" == true ]]; then
    new_core_root=$payload_directory/Core_Root
    mv $core_root_directory $new_core_root
fi

if [[ "$use_baseline_core_run" == true ]]; then
  new_baseline_core_root=$payload_directory/Baseline_Core_Root
  mv $baseline_core_root_directory $new_baseline_core_root
fi

if [[ "$iosmono" == "true" || "$iosnativeaot" == "true" ]]; then
  mkdir -p $payload_directory/iosHelloWorld && cp -rv $source_directory/iosHelloWorld $payload_directory/iosHelloWorld
  mkdir -p $payload_directory/iosHelloWorldZip && cp -rv $source_directory/iosHelloWorldZip $payload_directory/iosHelloWorldZip

  find "$payload_directory/iosHelloWorldZip/" -type f -name "*.zip" -execdir mv {} "$payload_directory/iosHelloWorldZip/iOSSampleApp.zip" \;
fi

ci=true

_script_dir=$(pwd)/eng/common
. "$_script_dir/pipeline-logging-functions.sh"

# Prevent vso[task.setvariable to be erroneously processed
set +x

# Make sure all of our variables are available for future steps
Write-PipelineSetVariable -name "UseCoreRun" -value "$use_core_run" -is_multi_job_variable false
Write-PipelineSetVariable -name "UseBaselineCoreRun" -value "$use_baseline_core_run" -is_multi_job_variable false
Write-PipelineSetVariable -name "Architecture" -value "$architecture" -is_multi_job_variable false
Write-PipelineSetVariable -name "PayloadDirectory" -value "$payload_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "PerformanceDirectory" -value "$performance_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "WorkItemDirectory" -value "$workitem_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "Queue" -value "$queue" -is_multi_job_variable false
Write-PipelineSetVariable -name "SetupArguments" -value "$setup_arguments" -is_multi_job_variable false
Write-PipelineSetVariable -name "Python" -value "python3" -is_multi_job_variable false
Write-PipelineSetVariable -name "PerfLabArguments" -value "$perflab_arguments" -is_multi_job_variable false
Write-PipelineSetVariable -name "ExtraBenchmarkDotNetArguments" -value "$extra_benchmark_dotnet_arguments" -is_multi_job_variable false
Write-PipelineSetVariable -name "BDNCategories" -value "$run_categories" -is_multi_job_variable false
Write-PipelineSetVariable -name "TargetCsproj" -value "$csproj" -is_multi_job_variable false
Write-PipelineSetVariable -name "RunFromPerfRepo" -value "$run_from_perf_repo" -is_multi_job_variable false
Write-PipelineSetVariable -name "Creator" -value "$creator" -is_multi_job_variable false
Write-PipelineSetVariable -name "HelixSourcePrefix" -value "$helix_source_prefix" -is_multi_job_variable false
Write-PipelineSetVariable -name "Kind" -value "$kind" -is_multi_job_variable false
Write-PipelineSetVariable -name "_BuildConfig" -value "$_BuildConfig" -is_multi_job_variable false
Write-PipelineSetVariable -name "Compare" -value "$compare" -is_multi_job_variable false
Write-PipelineSetVariable -name "MonoDotnet" -value "$using_mono" -is_multi_job_variable false
Write-PipelineSetVariable -name "MonoAOT" -value "$monoaot" -is_multi_job_variable false
Write-PipelineSetVariable -name "WasmDotnet" -value "$using_wasm" -is_multi_job_variable false
Write-PipelineSetVariable -Name 'iOSLlvmBuild' -Value "$iosllvmbuild" -is_multi_job_variable false
Write-PipelineSetVariable -Name 'iOSStripSymbols' -Value "$iosstripsymbols" -is_multi_job_variable false
Write-PipelineSetVariable -Name 'hybridGlobalization' -Value "$hybridglobalization" -is_multi_job_variable false
Write-PipelineSetVariable -Name 'RuntimeType' -Value "$runtimetype" -is_multi_job_variable false
Write-PipelineSetVariable -name "OnlySanityCheck" -value "$only_sanity" -is_multi_job_variable false
Write-PipelineSetVariable -name "V8Version" -value "$v8_version" -is_multi_job_variable false

# Put it back to what was set on top of this script
set -x
