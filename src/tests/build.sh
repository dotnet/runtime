#!/usr/bin/env bash

build_Tests()
{
    echo "${__MsgPrefix}Building Tests..."

    __ProjectFilesDir="$__TestDir"
    __Exclude="$__RepoRootDir/src/tests/issues.targets"

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
    echo "__BuildArch: ${__BuildArch}"
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

    if [[ "$__SkipNative" != 1 && "$__BuildTestWrappersOnly" != 1 && "$__GenerateLayoutOnly" != 1 && "$__CopyNativeTestBinaries" != 1 && \
        "$__TargetOS" != "Browser" && "$__TargetOS" != "Android" && "$__TargetOS" != "iOS" && "$__TargetOS" != "iOSSimulator" ]]; then
        build_native "$__TargetOS" "$__BuildArch" "$__TestDir" "$__NativeTestIntermediatesDir" "install" "CoreCLR test component"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: native test build failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    # Set up directories and file names
    __BuildLog="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.log"
    __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.wrn"
    __BuildErr="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.err"
    __BuildBinLog="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.binlog"
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
    export __SkipTestWrappers
    export __BuildTestProject
    export __BuildTestDir
    export __BuildTestTree
    export __RuntimeFlavor
    export __CopyNativeProjectsAfterCombinedTestBuild
    export __CopyNativeTestBinaries
    export __Priority
    export __CreatePerfmap
    export __CompositeBuildMode
    export __BuildTestWrappersOnly
    export __GenerateLayoutOnly
    export __TestBuildMode
    export __MonoAot
    export __MonoFullAot
    export __MonoBinDir
    export __MsgPrefix
    export __ErrMsgPrefix
    export __Exclude

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

usage_list=()

usage_list+=("-skiprestorepackages: skip package restore.")
usage_list+=("-skipgeneratelayout: Do not generate the Core_Root layout.")
usage_list+=("-skiptestwrappers: Don't generate test wrappers.")

usage_list+=("-buildtestwrappersonly: only build the test wrappers.")
usage_list+=("-copynativeonly: Only copy the native test binaries to the managed output. Do not build the native or managed tests.")
usage_list+=("-generatelayoutonly: only pull down dependencies and build coreroot.")

usage_list+=("-test:xxx - only build a single test project");
usage_list+=("-dir:xxx - build all tests in a given directory");
usage_list+=("-tree:xxx - build all tests in a given subtree");

usage_list+=("-crossgen2: Precompiles the framework managed assemblies in coreroot using the Crossgen2 compiler.")
usage_list+=("-nativeaot: Builds the tests for Native AOT compilation.")
usage_list+=("-priority1: include priority=1 tests in the build.")
usage_list+=("-composite: Use Crossgen2 composite mode (all framework gets compiled into a single native R2R library).")
usage_list+=("-perfmap: emit perfmap symbol files when compiling the framework assemblies using Crossgen2.")
usage_list+=("-allTargets: Build managed tests for all target platforms (including test projects in which CLRTestTargetUnsupported resolves to true).")

usage_list+=("-rebuild: if tests have already been built - rebuild them.")
usage_list+=("-runtests: run tests after building them.")
usage_list+=("-mono: Build the tests for the Mono runtime honoring mono-specific issues.")

usage_list+=("-log: base file name to use for log files (used in lab pipelines that build tests in multiple steps to retain logs for each step.")

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRootDir="$(cd "$__ProjectRoot"/../..; pwd -P)"
__BuildArch=

handle_arguments_local() {
    case "$1" in
        buildtestwrappersonly|-buildtestwrappersonly)
            __BuildTestWrappersOnly=1
            ;;

        skiptestwrappers|-skiptestwrappers)
            __SkipTestWrappers=1
            ;;

        copynativeonly|-copynativeonly)
            __SkipNative=1
            __CopyNativeTestBinaries=1
            __CopyNativeProjectsAfterCombinedTestBuild=false
            __SkipGenerateLayout=1
            __SkipTestWrappers=1
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

        allTargets|-allTargets)
            __UnprocessedBuildArgs+=("/p:CLRTestBuildAllTargets=allTargets")
            ;;

        rebuild|-rebuild)
            __RebuildTests=1
            ;;

        test*|-test*)
            local arg="$1"
            local parts=(${arg//:/ })
            __BuildTestProject="$__BuildTestProject${parts[1]}%3B"
            ;;

        dir*|-dir*)
            local arg="$1"
            local parts=(${arg//:/ })
            __BuildTestDir="$__BuildTestDir${parts[1]}%3B"
            ;;

        tree*|-tree*)
            local arg="$1"
            local parts=(${arg//:/ })
            __BuildTestTree="$__BuildTestTree${parts[1]}%3B"
            ;;

        runtests|-runtests)
            __RunTests=1
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

        log*|-log*)
            local arg="$1"
            local parts=(${arg//:/ })
            __BuildLogRootName="${parts[1]}"
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

__SkipTestWrappers=0
__BuildTestWrappersOnly=0
__Compiler=clang
__ConfigureOnly=0
__CopyNativeProjectsAfterCombinedTestBuild=true
__CopyNativeTestBinaries=0
__CrossBuild=0
__DistroRid=""
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
__UseNinja=0
__VerboseBuild=0
__CMakeArgs=""
__Priority=0
__Mono=0
__MonoAot=0
__MonoFullAot=0
__BuildLogRootName="TestBuild"
CORE_ROOT=

source $__RepoRootDir/src/coreclr/_build-commons.sh

if [[ "${__BuildArch}" != "${__HostArch}" ]]; then
    __CrossBuild=1
fi

if [[ "$__CrossBuild" == 1 && "$__TargetOS" != "Android" ]]; then
    __UnprocessedBuildArgs+=("/p:CrossBuild=true")
fi

if [[ $__Mono -eq 1 ]]; then
    __RuntimeFlavor="mono"
else
    __RuntimeFlavor="coreclr"
fi

# Get the number of processors available to the scheduler
# Other techniques such as `nproc` only get the number of
# processors available to a single process.
__Platform="$(uname)"
if [[ "$__Platform" == "FreeBSD" ]]; then
    __NumProc=$(($(sysctl -n hw.ncpu)+1))
elif [[ "$__Platform" == "NetBSD" || "$__Platform" == "SunOS" ]]; then
    __NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
elif [[ "$__Platform" == "Darwin" ]]; then
    __NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
else
    __NumProc=$(nproc --all)
fi

# Set dependent variables
__LogsDir="$__RootBinDir/log"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__OSPlatformConfig="$__TargetOS.$__BuildArch.$__BuildType"
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
echo "${__MsgPrefix}Test binaries are available at ${__TestBinDir}"

if [[ "$__RunTests" -ne 0 ]]; then

    echo "Run Tests..."

    nextCommand="$__TestDir/run.sh --testRootDir=$__TestBinDir"
    echo "$nextCommand"
    eval $nextCommand

    echo "Tests run successful."
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
