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

usage_list+=("-nopgooptimize: do not use profile guided optimizations.")
usage_list+=("-pgoinstrument: generate instrumented code for profile guided optimization enabled binaries.")
usage_list+=("-skipcrossarchnative: Skip building cross-architecture native binaries.")
usage_list+=("-staticanalyzer: use scan_build static analyzer.")
usage_list+=("-component: Build individual components instead of the full project. Available options are 'jit', 'runtime', 'paltests', 'alljits', and 'iltools'. Can be specified multiple times.")

setup_dirs_local()
{
    setup_dirs

    mkdir -p "$__LogsDir"
    mkdir -p "$__MsbuildDebugLogsDir"

    if [[ "$__CrossBuild" == 1 ]]; then
        mkdir -p "$__CrossComponentBinDir"
    fi
}

restore_optdata()
{
    local OptDataProjectFilePath="$__ProjectRoot/.nuget/optdata/optdata.csproj"

    if [[ "$__PgoOptimize" == 1 && "$__IsMSBuildOnNETCoreSupported" == 1 ]]; then
        # Parse the optdata package versions out of msbuild so that we can pass them on to CMake

        local PgoDataPackagePathOutputFile="${__IntermediatesDir}/optdatapath.txt"

        local RestoreArg=""

        if [[ "$__SkipRestoreOptData" == "0" ]]; then
            RestoreArg="/restore"
        fi

        # Writes into ${PgoDataPackagePathOutputFile}
        "$__RepoRootDir/eng/common/msbuild.sh" /clp:nosummary $__ArcadeScriptArgs $OptDataProjectFilePath $RestoreArg /t:DumpPgoDataPackagePath \
                                            ${__CommonMSBuildArgs} /p:PgoDataPackagePathOutputFile=${PgoDataPackagePathOutputFile} \
                                            -bl:"$__LogsDir/PgoVersionRead_$__ConfigTriplet.binlog" > /dev/null 2>&1
        local exit_code="$?"
        if [[ "$exit_code" != 0 || ! -f "${PgoDataPackagePathOutputFile}" ]]; then
            echo "${__ErrMsgPrefix}Failed to get PGO data package path."
            exit "$exit_code"
        fi

        __PgoOptDataPath=$(<"${PgoDataPackagePathOutputFile}")
    fi
}

build_cross_architecture_components()
{
    local intermediatesForBuild="$__IntermediatesDir/Host$__CrossArch/crossgen"
    local crossArchBinDir="$__BinDir/$__CrossArch"

    mkdir -p "$intermediatesForBuild"
    mkdir -p "$crossArchBinDir"

    __SkipCrossArchBuild=1
    # check supported cross-architecture components host(__HostArch)/target(__BuildArch) pair
    if [[ ("$__BuildArch" == "arm" || "$__BuildArch" == "armel") && ("$__CrossArch" == "x86" || "$__CrossArch" == "x64") ]]; then
        __SkipCrossArchBuild=0
    elif [[ "$__BuildArch" == "arm64" && "$__CrossArch" == "x64" ]]; then
        __SkipCrossArchBuild=0
    else
        # not supported
        return
    fi

    __CMakeBinDir="$crossArchBinDir"
    CROSSCOMPILE=0
    export __CMakeBinDir CROSSCOMPILE

    __CMakeArgs="-DCLR_CMAKE_TARGET_ARCH=$__BuildArch -DCLR_CROSS_COMPONENTS_BUILD=1 $__CMakeArgs"
    build_native "$__TargetOS" "$__CrossArch" "$__ProjectRoot" "$intermediatesForBuild" "crosscomponents" "$__CMakeArgs" "cross-architecture components"

    CROSSCOMPILE=1
    export CROSSCOMPILE
}

handle_arguments_local() {
    case "$1" in

        nopgooptimize|-nopgooptimize)
            __PgoOptimize=0
            __SkipRestoreOptData=1
            ;;

        pgoinstrument|-pgoinstrument)
            __PgoInstrument=1
            ;;

        skipcrossarchnative|-skipcrossarchnative)
            __SkipCrossArchNative=1
            ;;

        staticanalyzer|-staticanalyzer)
            __StaticAnalyzer=1
            ;;

        component|-component)
            __RequestedBuildComponents="$__RequestedBuildComponents $2"
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

__BuildArch=
__BuildType=Debug
__CodeCoverage=0

# Set the various build properties here so that CMake and MSBuild can pick them up
__Compiler=clang
__CompilerMajorVersion=
__CompilerMinorVersion=
__CommonMSBuildArgs=
__ConfigureOnly=0
__CrossBuild=0
__DistroRid=""
__PgoInstrument=0
__PgoOptDataPath=""
__PgoOptimize=1
__PortableBuild=1
__ProjectDir="$__ProjectRoot"
__RootBinDir="$__RepoRootDir/artifacts"
__SignTypeArg=""
__SkipConfigure=0
__SkipNative=0
__SkipCrossArchNative=0
__SkipGenerateVersion=0
__SkipManaged=0
__SkipRestore=""
__SkipRestoreOptData=0
__SourceDir="$__ProjectDir/src"
__StaticAnalyzer=0
__UnprocessedBuildArgs=
__UseNinja=0
__VerboseBuild=0
__CMakeArgs=""
__RequestedBuildComponents=""

source "$__ProjectRoot"/_build-commons.sh

# Set dependent variables

# Set the remaining variables based upon the determined build configuration
__LogsDir="$__RootBinDir/log/$__BuildType"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"
__ConfigTriplet="$__TargetOS.$__BuildArch.$__BuildType"
__BinDir="$__RootBinDir/bin/coreclr/$__ConfigTriplet"
__ArtifactsIntermediatesDir="$__RepoRootDir/artifacts/obj/coreclr"
__IntermediatesDir="$__ArtifactsIntermediatesDir/$__ConfigTriplet"

export __IntermediatesDir __ArtifactsIntermediatesDir
__CrossComponentBinDir="$__BinDir"

__CrossArch="$__HostArch"
if [[ "$__CrossBuild" == 1 ]]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
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

# Restore the package containing profile counts for profile-guided optimizations
restore_optdata

# Build the coreclr (native) components.
__CMakeArgs="-DCLR_CMAKE_PGO_INSTRUMENT=$__PgoInstrument -DCLR_CMAKE_OPTDATA_PATH=$__PgoOptDataPath -DCLR_CMAKE_PGO_OPTIMIZE=$__PgoOptimize $__CMakeArgs"

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

if [[ "$__SkipNative" == 1 ]]; then
    echo "Skipping CoreCLR component build."
else
    build_native "$__TargetOS" "$__BuildArch" "$__ProjectRoot" "$__IntermediatesDir" "$__CMakeTarget" "$__CMakeArgs" "CoreCLR component"

    # Build cross-architecture components
    if [[ "$__SkipCrossArchNative" != 1 ]]; then
        if [[ "$__CrossBuild" == 1 ]]; then
            build_cross_architecture_components
        fi
    fi
fi

# Build complete

echo "Repo successfully built."
echo "Product binaries are available at $__BinDir"
exit 0
