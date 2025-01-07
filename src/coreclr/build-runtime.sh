#!/usr/bin/env bash

# resolve python-version to use
if [[ -z "$PYTHON" ]]; then
    if ! PYTHON=$(command -v python3 || command -v python2 || command -v python || command -v py)
    then
       echo "Unable to locate build-dependency python!" 1>&2
       exit 1
    fi
fi
# validate python-dependency
# useful in case of explicitly set option.
if ! command -v "$PYTHON" > /dev/null
then
   echo "Unable to locate build-dependency python ($PYTHON)!" 1>&2
   exit 1
fi

export PYTHON

usage_list+=("-pgodatapath: path to profile guided optimization data.")
usage_list+=("-pgoinstrument: generate instrumented code for profile guided optimization enabled binaries.")
usage_list+=("-staticanalyzer: use scan_build static analyzer.")
usage_list+=("-component: Build individual components instead of the full project. Available options are 'hosts', 'jit', 'runtime', 'paltests', 'alljits', 'iltools', 'nativeaot', and 'spmi'. Can be specified multiple times.")
usage_list+=("-subdir: Append a directory with the provided name to the obj and bin paths.")

setup_dirs_local()
{
    setup_dirs

    mkdir -p "$__LogsDir"
    mkdir -p "$__MsbuildDebugLogsDir"
}

handle_arguments_local() {
    case "$1" in

        pgodatapath|-pgodatapath)
            __PgoOptimize=1
            __PgoOptDataPath=$2
            __ShiftArgs=1
            ;;

        pgoinstrument|-pgoinstrument)
            __PgoInstrument=1
            ;;

        staticanalyzer|-staticanalyzer)
            __StaticAnalyzer=1
            ;;

        component|-component)
            __RequestedBuildComponents="$__RequestedBuildComponents $2"
            __ShiftArgs=1
            ;;
        
        subdir|-subdir)
            __SubDir="$2"
            __ShiftArgs=1
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac
}

echo "Commencing CoreCLR Repo build"

# Argument types supported by this script:
#
# Build architecture - valid values are: x64, ARM.
# Build Type         - valid values are: Debug, Checked, Release
#
# Set the default arguments for build

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRootDir="$(cd "$__ProjectRoot"/../..; pwd -P)"

__TargetArch=
__BuildType=Debug
__CodeCoverage=0

# Set the various build properties here so that CMake and MSBuild can pick them up
__Compiler=clang
__CommonMSBuildArgs=
__ConfigureOnly=0
__CrossBuild=0
__PgoInstrument=0
__PgoOptDataPath=""
__PgoOptimize=0
__PortableBuild=1
__ProjectDir="$__ProjectRoot"
__RootBinDir="$__RepoRootDir/artifacts"
__SignTypeArg=""
__SkipConfigure=0
__SkipRestore=""
__SourceDir="$__ProjectDir/src"
__StaticAnalyzer=0
__UnprocessedBuildArgs=
__UseNinja=0
__VerboseBuild=0
__CMakeArgs=""
__RequestedBuildComponents=""
__SubDir=""

source "$__ProjectRoot"/_build-commons.sh

# Set dependent variables

# Set the remaining variables based upon the determined build configuration
__LogsDir="$__RootBinDir/log/$__BuildType"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"
__ConfigTriplet="$__TargetOS.$__TargetArch.$__BuildType"
if [[ "$__TargetOS" == "linux-bionic" ]]; then
    __ConfigTriplet="linux.$__TargetArch.$__BuildType"
fi
__BinDir="$__RootBinDir/bin/coreclr/$__ConfigTriplet"
__ArtifactsObjDir="$__RepoRootDir/artifacts/obj"
__ArtifactsIntermediatesDir="$__ArtifactsObjDir/coreclr"
__IntermediatesDir="$__ArtifactsIntermediatesDir/$__ConfigTriplet"

export __IntermediatesDir __ArtifactsIntermediatesDir

if [[ "$__ExplicitHostArch" == 1 ]]; then
    __IntermediatesDir="$__IntermediatesDir/$__HostArch"
    __BinDir="$__BinDir/$__HostArch"
fi

if [[ -n "$__SubDir" ]]; then
    __IntermediatesDir="$__IntermediatesDir/$__SubDir"
    __BinDir="$__BinDir/$__SubDir"
fi

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to set.
# This is needed by CLI to function.
if [[ -z "$HOME" ]]; then
    if [[ ! -d "$__ProjectDir/temp_home" ]]; then
        mkdir temp_home
    fi
    HOME="$__ProjectDir"/temp_home
    export HOME
    echo "HOME not defined; setting it to $HOME"
fi

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built CoreClr libraries will copied to.
__CMakeBinDir="$__BinDir"
export __CMakeBinDir

# Make the directories necessary for build if they don't exist
setup_dirs_local

# Set up the directory for MSBuild debug logs.
MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"
export MSBUILDDEBUGPATH

# Check prereqs.
check_prereqs

# Build the coreclr (native) components.
__CMakeArgs="-DCLR_CMAKE_PGO_INSTRUMENT=$__PgoInstrument -DCLR_CMAKE_OPTDATA_PATH=$__PgoOptDataPath -DCLR_CMAKE_PGO_OPTIMIZE=$__PgoOptimize -DCLI_CMAKE_FALLBACK_OS=\"$__HostFallbackOS\" $__CMakeArgs"

if [[ "$__SkipConfigure" == 0 && "$__CodeCoverage" == 1 ]]; then
    __CMakeArgs="-DCLR_CMAKE_ENABLE_CODE_COVERAGE=1 $__CMakeArgs"
fi

__CMakeTarget=""
if [[ -n "$__RequestedBuildComponents" ]]; then
    __CMakeTarget=" $__RequestedBuildComponents "
    __CMakeTarget="${__CMakeTarget// paltests / paltests_install }"
fi
if [[ -z "$__CMakeTarget" ]]; then
    __CMakeTarget="install"
fi

if [[ "$__TargetArch" != "$__HostArch" ]]; then
    __CMakeArgs="-DCLR_CMAKE_TARGET_ARCH=$__TargetArch $__CMakeArgs"
fi

if [[ "$USE_SCCACHE" == "true" ]]; then
    __CMakeArgs="-DCMAKE_C_COMPILER_LAUNCHER=sccache -DCMAKE_CXX_COMPILER_LAUNCHER=sccache $__CMakeArgs"
fi

eval "$__RepoRootDir/eng/native/version/copy_version_files.sh"

build_native "$__HostOS" "$__HostArch" "$__ProjectRoot" "$__IntermediatesDir" "$__CMakeTarget" "$__CMakeArgs" "CoreCLR component"

# Build complete

echo "Repo successfully built."
echo "Product binaries are available at $__BinDir"
exit 0
