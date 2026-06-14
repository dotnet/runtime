#!/usr/bin/env bash

build_Tests()
{
    echo "${__MsgPrefix}Building Tests..."

    if [[ "$__UseBxl" -eq 1 ]]; then
        build_Tests_WithBxl
        return
    fi

    __ProjectFilesDir="$__TestDir"

    if [[ -f  "${__TestBinDir}/build_info.json" ]]; then
        rm  "${__TestBinDir}/build_info.json"
    fi

    if [[ "$__RebuildTests" -ne 0 ]]; then
        if [[ -d "$__TestBinDir" ]]; then
            echo "Removing tests build dir: ${__TestBinDir}"
            rm -rf "$__TestBinDir"
        fi
    fi

    __CMakeBinDir="$__TestBinDir"
    export __CMakeBinDir

    if [[ ! -d "$__TestIntermediatesDir" ]]; then
        mkdir -p "$__TestIntermediatesDir"
    fi

    __NativeTestIntermediatesDir="${__TestIntermediatesDir}/Native"
    if [[  ! -d "${__NativeTestIntermediatesDir}" ]]; then
        mkdir -p "${__NativeTestIntermediatesDir}"
    fi

    __ManagedTestIntermediatesDir="${__TestIntermediatesDir}/Managed"
    if [[ ! -d "${__ManagedTestIntermediatesDir}" ]]; then
        mkdir -p "${__ManagedTestIntermediatesDir}"
    fi

    echo "__TargetOS: ${__TargetOS}"
    echo "__TargetArch: ${__TargetArch}"
    echo "__BuildType: ${__BuildType}"
    echo "__TestIntermediatesDir: ${__TestIntermediatesDir}"
    echo "__NativeTestIntermediatesDir: ${__NativeTestIntermediatesDir}"
    echo "__ManagedTestIntermediatesDir: ${__ManagedTestIntermediatesDir}"

    if [[ ! -f "$__TestBinDir" ]]; then
        echo "Creating TestBinDir: ${__TestBinDir}"
        mkdir -p "$__TestBinDir"
    fi
    if [[ ! -f "$__LogsDir" ]]; then
        echo "Creating LogsDir: ${__LogsDir}"
        mkdir -p "$__LogsDir"
    fi
    if [[ ! -f "$__MsbuildDebugLogsDir" ]]; then
        echo "Creating MsbuildDebugLogsDir: ${__MsbuildDebugLogsDir}"
        mkdir -p "$__MsbuildDebugLogsDir"
    fi

    # Set up the directory for MSBuild debug logs.
    MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"
    export MSBUILDDEBUGPATH

    if [[ "$__SkipNative" != 1 && "$__GenerateLayoutOnly" != 1 && "$__CopyNativeTestBinaries" != 1 && \
        "$__TargetOS" != "android" && "$__TargetOS" != "ios" && "$__TargetOS" != "iossimulator" && "$__TargetOS" != "tvos" && "$__TargetOS" != "tvossimulator" ]]; then
        build_native "$__TargetOS" "$__TargetArch" "$__TestDir" "$__NativeTestIntermediatesDir" "install" "$__CMakeArgs" "CoreCLR test component"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: native test build failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    # Set up directories and file names
    __BuildLog="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__TargetArch}.${__BuildType}.log"
    __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__TargetArch}.${__BuildType}.wrn"
    __BuildErr="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__TargetArch}.${__BuildType}.err"
    __BuildBinLog="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__TargetArch}.${__BuildType}.binlog"
    __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog}\""
    __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn}\""
    __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr}\""

    # Uncomment the line below when instrumenting Linux builds to produce binlogs;
    # they seem to be too lengthy to be enabled by default.
    __msbuildBinLog="\"/bl:${__BuildBinLog}\""

    # Export properties as environment variables for the MSBuild scripts to use
    export __TestDir
    export __TestIntermediatesDir
    export __NativeTestIntermediatesDir
    export __BinDir
    export __TestBinDir
    export __SkipManaged
    export __SkipRestorePackages
    export __SkipGenerateLayout
    export __BuildTestProject
    export __BuildTestDir
    export __BuildTestTree
    export __RuntimeFlavor
    export __CopyNativeProjectsAfterCombinedTestBuild
    export __CopyNativeTestBinaries
    export __Priority
    export __CreatePerfmap
    export __CompositeBuildMode
    export __GenerateLayoutOnly
    export __TestBuildMode
    export __MonoAot
    export __MonoFullAot
    export __MonoBinDir
    export __MsgPrefix
    export __ErrMsgPrefix
    export EnableNativeSanitizers

    # Generate build command
    buildArgs=("$__RepoRootDir/src/tests/build.proj")
    buildArgs+=("/t:TestBuild")
    buildArgs+=("${__CommonMSBuildArgs}")
    buildArgs+=("/maxcpucount")
    buildArgs+=("${__msbuildLog}" "${__msbuildWrn}" "${__msbuildErr}" "${__msbuildBinLog}")
    buildArgs+=("/p:NUMBER_OF_PROCESSORS=${__NumProc}")
    buildArgs+=("${__UnprocessedBuildArgs[@]}")

    # Disable warnAsError - https://github.com/dotnet/runtime/issues/11077
    nextCommand="\"$__RepoRootDir/eng/common/msbuild.sh\" $__ArcadeScriptArgs --warnAsError false ${buildArgs[@]}"
    echo "Building tests via $nextCommand"
    eval $nextCommand

    # Make sure everything is OK
    if [[ "$?" -ne 0 ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}Failed to build tests. See the build logs:"
        echo "    $__BuildLog"
        echo "    $__BuildWrn"
        echo "    $__BuildErr"
        echo "    $__BuildBinLog"
        exit 1
    fi
}

resolve_bxl_test_path() {
    local input_path="$1"
    local resolved_path=

    if [[ "$input_path" = /* ]]; then
        resolved_path="$(realpath -m "$input_path")"
    else
        resolved_path="$(realpath -m "$__TestDir/$input_path")"
    fi

    if [[ ! -e "$resolved_path" ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL path does not exist: $input_path" >&2
        return 1
    fi

    if [[ "$resolved_path" != "$__TestDir" && "$resolved_path" != "$__TestDir/"* ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL paths must stay under $__TestDir: $input_path" >&2
        return 1
    fi

    printf '%s\n' "$resolved_path"
}

find_bxl_spec_for_path() {
    local input_path="$1"
    local resolved_path=
    local probe_dir=

    if ! resolved_path="$(resolve_bxl_test_path "$input_path")"; then
        return 1
    fi
    if [[ -d "$resolved_path" ]]; then
        probe_dir="$resolved_path"
    else
        probe_dir="$(dirname "$resolved_path")"
    fi

    while [[ "$probe_dir" == "$__TestDir" || "$probe_dir" == "$__TestDir/"* ]]; do
        if [[ -f "$probe_dir/BUILD.dsc" ]]; then
            printf '%s\n' "$probe_dir/BUILD.dsc"
            return
        fi

        if [[ "$probe_dir" == "$__TestDir" ]]; then
            break
        fi

        probe_dir="$(dirname "$probe_dir")"
    done

    echo "${__ErrMsgPrefix}${__MsgPrefix}No BUILD.dsc found for BuildXL path: $input_path" >&2
    return 1
}

append_bxl_filter_term() {
    local expression="$1"

    if [[ -z "$expression" ]]; then
        return
    fi

    if [[ -z "$__BxlFilterExpression" ]]; then
        __BxlFilterExpression="$expression"
    else
        __BxlFilterExpression="(${__BxlFilterExpression})or(${expression})"
    fi
}

append_bxl_encoded_spec_filters() {
    local encoded_values="$1"
    local mode="$2"
    local decoded_values=
    local item=
    local resolved_path=
    local spec_path=

    decoded_values="${encoded_values//%3B/$'\n'}"

    while IFS= read -r item; do
        [[ -n "$item" ]] || continue

        case "$mode" in
            test|dir)
                if ! spec_path="$(find_bxl_spec_for_path "$item")"; then
                    exit 1
                fi
                append_bxl_filter_term "spec='${spec_path}'"
                ;;
            tree)
                if ! resolved_path="$(resolve_bxl_test_path "$item")"; then
                    exit 1
                fi
                append_bxl_filter_term "spec='${resolved_path}/*'"
                ;;
        esac
    done <<< "$decoded_values"
}

build_bxl_selector_filter() {
    __BxlFilterExpression=
    append_bxl_encoded_spec_filters "$__BuildTestProject" "test"
    append_bxl_encoded_spec_filters "$__BuildTestDir" "dir"
    append_bxl_encoded_spec_filters "$__BuildTestTree" "tree"
}

validate_bxl_build_configuration() {
    if [[ "$__TargetOS" != "linux" ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow currently supports only -os linux."
        exit 1
    fi

    if [[ "$__TargetArch" != "x64" ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow currently supports only x64."
        exit 1
    fi

    if [[ "$__BuildType" != "Checked" ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow currently supports only Checked runtime configuration."
        exit 1
    fi

    if [[ "$__RuntimeFlavor" != "coreclr" ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow currently supports only CoreCLR."
        exit 1
    fi

    if [[ "$__CrossBuild" == 1 ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow does not support cross-build test execution."
        exit 1
    fi

    if [[ "$__SkipManaged" == 1 ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow cannot skip managed test compilation."
        exit 1
    fi

    if [[ "$__CopyNativeTestBinaries" == 1 || "$__GenerateLayoutOnly" == 1 || "$__SkipGenerateLayout" == 1 ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow does not support native-copy or layout-only modes."
        exit 1
    fi

    if [[ -n "$__TestBuildMode" || -n "$__CompositeBuildMode" || -n "$__CreatePerfmap" ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow does not support crossgen2, composite, nativeaot, or perfmap test modes."
        exit 1
    fi

    if [[ "$__SkipRestorePackages" == 1 || ${#__UnprocessedBuildArgs[@]} -ne 0 ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow does not accept restore-skipping or direct MSBuild arguments."
        exit 1
    fi
}

validate_bxl_core_root() {
    local bxl_core_root="$__RepoRootDir/artifacts/tests/coreclr/linux.x64.Checked/Tests/Core_Root"

    if [[ -x "$bxl_core_root/corerun" && -x "$bxl_core_root/ilasm" ]]; then
        return
    fi

    echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test flow requires a Checked Core_Root at:" >&2
    echo "    $bxl_core_root" >&2
    echo "${__ErrMsgPrefix}${__MsgPrefix}Generate it with the normal CoreCLR test setup first:" >&2
    echo "    ./build.sh clr+libs+clr.iltools -lc Release -rc Checked" >&2
    echo "    src/tests/build.sh checked x64 generatelayoutonly" >&2
    exit 1
}

run_bxl_command() {
    local command="$1"

    if [[ -n "$__BxlFilterExpression" ]]; then
        BXL_FILTER_APPEND="$__BxlFilterExpression" bash "$__RepoRootDir/bxl.sh" "$command"
    else
        bash "$__RepoRootDir/bxl.sh" "$command"
    fi
}

build_Tests_WithBxl() {
    validate_bxl_build_configuration
    validate_bxl_core_root
    build_bxl_selector_filter

    if [[ "$__RebuildTests" -ne 0 && -d "$__RepoRootDir/Out" ]]; then
        echo "Removing BuildXL output dir: $__RepoRootDir/Out"
        rm -rf "$__RepoRootDir/Out"
    fi

    echo "__TargetOS: ${__TargetOS}"
    echo "__TargetArch: ${__TargetArch}"
    echo "__BuildType: ${__BuildType}"

    if [[ -n "$__BxlFilterExpression" ]]; then
        echo "BuildXL filter: ${__BxlFilterExpression}"
    fi

    echo "Building tests via BuildXL"
    run_bxl_command "build"

    if [[ "$?" -ne 0 ]]; then
        echo "${__ErrMsgPrefix}${__MsgPrefix}BuildXL test build failed. See Out/Logs/BuildXL.Dev.log for details."
        exit 1
    fi
}

usage_list=()
usage_list+=("All arguments are optional and the '-' prefix is optional. The options are:")
usage_list+=("")
usage_list+=("Build target OS: Use '-os <value>' (handled by the common build framework) to specify the")
usage_list+=("    target OS. Common values: linux (default on Linux), osx (default on macOS), android,")
usage_list+=("    ios, iossimulator, tvos, tvossimulator, maccatalyst, browser, wasi.")
usage_list+=("    For mobile/device targets (android, ios, iossimulator, tvos, tvossimulator), this script")
usage_list+=("    automatically skips building native test components.")
usage_list+=("-browser - Shorthand for '-os browser' (also sets architecture to wasm).")
usage_list+=("-wasi - Shorthand for '-os wasi' (also sets architecture to wasm).")
usage_list+=("")
usage_list+=("-rebuild - Clean up all test artifacts prior to building tests.")
usage_list+=("-skiprestorepackages - Skip package restore.")
usage_list+=("-skipmanaged - Skip the managed tests build.")
usage_list+=("-skipnative - Skip the native tests build.")
usage_list+=("-skipgeneratelayout - Skip generating the Core_Root layout.")
usage_list+=("")
usage_list+=("-copynativeonly - Only copy the native test binaries to the managed output. Do not build the native or managed tests.")
usage_list+=("-generatelayoutonly - Only generate the Core_Root layout without building managed or native test components.")
usage_list+=("")
usage_list+=("-crossgen2 - Precompiles the framework managed assemblies in coreroot using the Crossgen2 compiler.")
usage_list+=("-composite - Use Crossgen2 composite mode (all framework gets compiled into a single native R2R library).")
usage_list+=("-nativeaot - Builds the tests for Native AOT compilation.")
usage_list+=("-priority1 - Include priority=1 tests in the build.")
usage_list+=("-perfmap - Emit perfmap symbol files when compiling the framework assemblies using Crossgen2.")
usage_list+=("-allTargets - Build managed tests for all target platforms (including test projects in which CLRTestTargetUnsupported resolves to true).")
usage_list+=("-use-bootstrap - Use artifacts produced by the bootstrap subset for local targeting, runtime, and apphost packs.")
usage_list+=("")
usage_list+=("-runtests - Run tests after building them.")
usage_list+=("-bxl - Build tests via the BuildXL-backed flow (Linux x64 Checked CoreCLR with Release libraries only).")
usage_list+=("-mono, -excludemonofailures - Build the tests for the Mono runtime honoring mono-specific issues.")
usage_list+=("-coreclr - Build tests targeting the CoreCLR runtime (default; opposite of -mono/-excludemonofailures).")
usage_list+=("-mono_aot - Use Mono AOT mode.")
usage_list+=("-mono_fullaot - Use Mono Full AOT mode.")
usage_list+=("")
usage_list+=("-test:xxx - Only build the specified test project (relative or absolute project path under src/tests).")
usage_list+=("-dir:xxx - Build all test projects in the given directory (relative or absolute directory under src/tests).")
usage_list+=("-tree:xxx - Build all test projects in the given subtree (relative or absolute directory under src/tests).")
usage_list+=("-log:xxx - Base file name to use for log files (used in lab pipelines that build tests in multiple steps to retain logs for each step).")
usage_list+=("")
usage_list+=("Any unrecognized arguments will be passed directly to MSBuild.")

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRootDir="$(cd "$__ProjectRoot"/../..; pwd -P)"
__TargetArch=

handle_arguments_local() {
    opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"

    case "$opt" in
        skipmanaged|-skipmanaged)
            __SkipManaged=1
            ;;

        skipnative|-skipnative)
            __SkipNative=1
            __CopyNativeProjectsAfterCombinedTestBuild=false
            ;;

        copynativeonly|-copynativeonly)
            __SkipNative=1
            __CopyNativeTestBinaries=1
            __CopyNativeProjectsAfterCombinedTestBuild=false
            __SkipGenerateLayout=1
            ;;

        crossgen2|-crossgen2)
            __TestBuildMode=crossgen2
            ;;

        composite|-composite)
            __CompositeBuildMode=1
            __TestBuildMode=crossgen2
            ;;

        nativeaot|-nativeaot)
            __TestBuildMode=nativeaot
            ;;

        perfmap|-perfmap)
            __CreatePerfmap=1
            ;;

        generatelayoutonly|-generatelayoutonly)
            __GenerateLayoutOnly=1
            ;;

        priority1|-priority1)
            __Priority=1
            ;;

        alltargets|-alltargets)
            __UnprocessedBuildArgs+=("/p:CLRTestBuildAllTargets=allTargets")
            ;;

        use-bootstrap|-use-bootstrap)
            __UnprocessedBuildArgs+=("/p:UseBootstrap=true")
            ;;

        rebuild|-rebuild)
            __RebuildTests=1
            ;;

        test*|-test*)
            local arg="$1"
            local parts=(${arg//:/ })
            if [[ ${#parts[@]} -eq 1 ]]; then
                __BuildTestProject="$__BuildTestProject$2%3B"
                __ShiftArgs=1
            else
                __BuildTestProject="$__BuildTestProject${parts[1]}%3B"
            fi
            ;;

        dir*|-dir*)
            local arg="$1"
            local parts=(${arg//:/ })
            if [[ ${#parts[@]} -eq 1 ]]; then
                __BuildTestDir="$__BuildTestDir$2%3B"
                __ShiftArgs=1
            else
                __BuildTestDir="$__BuildTestDir${parts[1]}%3B"
            fi
            ;;

        tree*|-tree*)
            local arg="$1"
            local parts=(${arg//:/ })
            if [[ ${#parts[@]} -eq 1 ]]; then
                __BuildTestTree="$__BuildTestTree$2%3B"
                __ShiftArgs=1
            else
                __BuildTestTree="$__BuildTestTree${parts[1]}%3B"
            fi
            ;;

        runtests|-runtests)
            __RunTests=1
            ;;

        bxl|-bxl)
            __UseBxl=1
            ;;

        skiprestorepackages|-skiprestorepackages)
            __SkipRestorePackages=1
            ;;

        skipgeneratelayout|-skipgeneratelayout)
            __SkipGenerateLayout=1
            ;;

        excludemonofailures|-excludemonofailures)
            __Mono=1
            ;;

        mono|-mono)
            __Mono=1
            ;;

        mono_aot|-mono_aot)
            __Mono=1
            __MonoAot=1
            __SkipNative=1
            ;;

        mono_fullaot|-mono_fullaot)
            __Mono=1
            __MonoFullAot=1
            __SkipNative=1
            ;;

        coreclr|-coreclr)
            __Mono=0
            __MonoAot=0
            __MonoFullAot=0
            ;;

        browser|-browser)
            __TargetOS=browser
            __TargetArch=wasm
            ;;

        wasi|-wasi)
            __TargetOS=wasi
            __TargetArch=wasm
            ;;

        log*|-log*)
            local arg="$1"
            local parts=(${arg//:/ })
            if [[ ${#parts[@]} -eq 1 ]]; then
                __BuildLogRootName="$2"
                __ShiftArgs=1
            else
                __BuildLogRootName="${parts[1]}"
            fi
            ;;

        *)
            __UnprocessedBuildArgs+=("$1")
            ;;
    esac
}

__BuildType=Debug
__CodeCoverage=
__IncludeTests=INCLUDE_TESTS

# Set the various build properties here so that CMake and MSBuild can pick them up
__ProjectDir="$__ProjectRoot"
export __ProjectDir

__Compiler=clang
__ConfigureOnly=0
__CopyNativeProjectsAfterCombinedTestBuild=true
__CopyNativeTestBinaries=0
__CrossBuild=0
__CompositeBuildMode=
__CreatePerfmap=
__TestBuildMode=
__BuildTestProject="%3B"
__BuildTestDir="%3B"
__BuildTestTree="%3B"
__DotNetCli="$__RepoRootDir/dotnet.sh"
__GenerateLayoutOnly=0
__MSBCleanBuildArgs=
__NativeTestIntermediatesDir=
__PortableBuild=1
__RebuildTests=0
__RootBinDir="$__RepoRootDir/artifacts"
__RunTests=0
__SkipConfigure=0
__SkipGenerateLayout=0
__SkipManaged=0
__SkipNative=0
__SkipRestore=""
__SkipRestorePackages=0
__SourceDir="$__ProjectDir/src"
__UnprocessedBuildArgs=()
__VerboseBuild=0
__CMakeArgs=""
__Priority=0
__Mono=0
__MonoAot=0
__MonoFullAot=0
__UseBxl=0
__BxlFilterExpression=
__BuildLogRootName="TestBuild"
CORE_ROOT=
EnableNativeSanitizers=

source $__RepoRootDir/src/coreclr/_build-commons.sh

if [[ "${__TargetArch}" != "${__HostArch}" ]]; then
    __CrossBuild=1
fi

if [[ "$__CrossBuild" == 1 && "$__TargetOS" != "android" ]]; then
    __UnprocessedBuildArgs+=("/p:CrossBuild=true")
fi

if [[ $__Mono -eq 1 ]]; then
    __RuntimeFlavor="mono"
else
    __RuntimeFlavor="coreclr"
fi

# Get the number of processors available to the scheduler
platform="$(uname -s | tr '[:upper:]' '[:lower:]')"
if [[ "$platform" == "freebsd" ]]; then
  __NumProc="$(($(sysctl -n hw.ncpu)+1))"
elif [[ "$platform" == "netbsd" || "$platform" == "sunos" ]]; then
  __NumProc="$(($(getconf NPROCESSORS_ONLN)+1))"
elif [[ "$platform" == "darwin" ]]; then
  __NumProc="$(($(getconf _NPROCESSORS_ONLN)+1))"
elif command -v nproc > /dev/null 2>&1; then
  __NumProc="$(nproc)"
elif (NAME=""; . /etc/os-release; test "$NAME" = "Tizen"); then
  __NumProc="$(getconf _NPROCESSORS_ONLN)"
else
  __NumProc=1
fi

# Set dependent variables
__LogsDir="$__RootBinDir/log"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__OSPlatformConfig="$__TargetOS.$__TargetArch.$__BuildType"
__BinDir="$__RootBinDir/bin/coreclr/$__OSPlatformConfig"
__PackagesBinDir="$__BinDir/.nuget"
__TestDir="$__RepoRootDir/src/tests"
__TestBinDir="$__RootBinDir/tests/coreclr/$__OSPlatformConfig"
__IntermediatesDir="$__RootBinDir/obj/coreclr/$__OSPlatformConfig"
__TestIntermediatesDir="$__RootBinDir/tests/coreclr/obj/$__OSPlatformConfig"
__CrossCompIntermediatesDir="$__IntermediatesDir/crossgen"
__MonoBinDir="$__RootBinDir/bin/mono/$__OSPlatformConfig"

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to it.
# This is needed by CLI to function.
if [[ -z "$HOME" ]]; then
    if [[ ! -d "$__ProjectDir/temp_home" ]]; then
        mkdir temp_home
    fi

    HOME="$__ProjectDir"/temp_home
    export HOME
    echo "HOME not defined; setting it to $HOME"
fi

if [[ "$__RebuildTests" -ne 0 ]]; then
    echo "Removing test build dir: ${__TestBinDir}"
    rm -rf "${__TestBinDir}"
    echo "Removing test intermediate dir: ${__TestIntermediatesDir}"
    rm -rf "${__TestIntermediatesDir}"
fi

build_Tests

if [[ "$?" -ne 0 ]]; then
    echo "Failed to build tests"
    exit 1
fi

echo "${__MsgPrefix}Test build successful."
if [[ "$__UseBxl" -eq 1 ]]; then
    echo "${__MsgPrefix}BuildXL outputs are available under ${__RepoRootDir}/Out"
else
    echo "${__MsgPrefix}Test binaries are available at ${__TestBinDir}"
fi

if [[ "$__RunTests" -ne 0 ]]; then
    exitCode=0

    echo "Run Tests..."

    if [[ "$__UseBxl" -eq 1 ]]; then
        nextCommand="$__TestDir/run.sh --bxl"
        if [[ -n "$__BxlFilterExpression" ]]; then
            nextCommand="$nextCommand \"--bxl-filter=${__BxlFilterExpression}\""
        fi
        nextCommand="$nextCommand checked x64"
    else
        nextCommand="$__TestDir/run.sh --testRootDir=$__TestBinDir"
    fi
    echo "$nextCommand"
    eval $nextCommand
    exitCode=$?

    if [[ "$exitCode" -ne 0 ]]; then
        exit "$exitCode"
    fi

    echo "Tests run successful."
else
    if [[ "$__UseBxl" -eq 1 ]]; then
        echo "To run the BuildXL-backed tests use:"
        echo ""
        echo "    src/tests/run.sh --bxl checked x64"
        echo ""
        echo "To run a BuildXL-backed subtree use:"
        echo ""
        echo "    src/tests/run.sh --bxl checked x64 --tree=JIT/Regression"
        echo ""
    else
        echo "To run all the tests use:"
        echo ""
        echo "    src/tests/run.sh $__BuildType"
        echo ""
        echo "To run a single test use:"
        echo ""
        echo "    bash ${__TestBinDir}/__TEST_PATH__/__TEST_NAME__.sh -coreroot=${CORE_ROOT}"
        echo ""
    fi
fi
