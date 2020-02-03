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

usage_list=("-crossgenonly: only run native image generation.")
usage_list+=("-disableoss: Disable Open Source Signing for System.Private.CoreLib.")
usage_list+=("-ibcinstrument: generate IBC-tuning-enabled native images when invoking crossgen.")
usage_list+=("-nopgooptimize: do not use profile guided optimizations.")
usage_list+=("-officialbuildid=^<ID^>: specify the official build ID to be used by this build.")
usage_list+=("-partialngen: build CoreLib as PartialNGen.")
usage_list+=("-pgoinstrument: generate instrumented code for profile guided optimization enabled binaries.")
usage_list+=("-skipcrossgen: skip native image generation.")
usage_list+=("-skipcrossarchnative: Skip building cross-architecture native binaries.")
usage_list+=("-skipmanagedtools: generate instrumented code for profile guided optimization enabled binaries.")
usage_list+=("-skipmscorlib: skip native image generation of System.Private.CoreLib.")
usage_list+=("-skiprestore: specify the official build ID to be used by this build.")
usage_list+=("-skiprestoreoptdata: build CoreLib as PartialNGen.")
usage_list+=("-staticanalyzer: skip native image generation.")

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
    local OptDataProjectFilePath="$__ProjectRoot/src/.nuget/optdata/optdata.csproj"
    if [[ "$__SkipRestoreOptData" == 0 && "$__IsMSBuildOnNETCoreSupported" == 1 ]]; then
        echo "Restoring the OptimizationData package"
        "$__RepoRootDir/eng/common/msbuild.sh" /clp:nosummary $__ArcadeScriptArgs \
                                               $OptDataProjectFilePath /t:Restore /m \
                                               $__CommonMSBuildArgs $__UnprocessedBuildArgs \
                                               /nodereuse:false
        local exit_code="$?"
        if [[ "$exit_code" != 0 ]]; then
            echo "${__ErrMsgPrefix}Failed to restore the optimization data package."
            exit "$exit_code"
        fi
    fi

    if [[ "$__IsMSBuildOnNETCoreSupported" == 1 ]]; then
        # Parse the optdata package versions out of msbuild so that we can pass them on to CMake

        local PgoDataPackagePathOutputFile="${__IntermediatesDir}/optdatapath.txt"

        # Writes into ${PgoDataPackagePathOutputFile}
        "$__RepoRootDir/eng/common/msbuild.sh" /clp:nosummary $__ArcadeScriptArgs $OptDataProjectFilePath /t:DumpPgoDataPackagePath ${__CommonMSBuildArgs} /p:PgoDataPackagePathOutputFile=${PgoDataPackagePathOutputFile} > /dev/null 2>&1
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
    build_native "$__CrossArch" "$__ProjectRoot" "$__ProjectRoot" "$intermediatesForBuild" "cross-architecture components"

    CROSSCOMPILE=1
    export CROSSCOMPILE
}

build_CoreLib_ni()
{
    local __CrossGenExec=$1
    local __CoreLibILDir=$2

    if [[ "$__PartialNgen" == 1 ]]; then
        COMPlus_PartialNGen=1
        export COMPlus_PartialNGen
    fi

    if [[ -e "$__CrossGenCoreLibLog" ]]; then
        rm "$__CrossGenCoreLibLog"
    fi
    echo "Generating native image of System.Private.CoreLib.dll for $__BuildOS.$__BuildArch.$__BuildType. Logging to \"$__CrossGenCoreLibLog\"."
    echo "$__CrossGenExec /Platform_Assemblies_Paths $__CoreLibILDir $__IbcTuning /out $__BinDir/System.Private.CoreLib.dll $__CoreLibILDir/System.Private.CoreLib.dll"
    "$__CrossGenExec" /nologo /Platform_Assemblies_Paths $__CoreLibILDir $__IbcTuning /out $__BinDir/System.Private.CoreLib.dll $__CoreLibILDir/System.Private.CoreLib.dll >> $__CrossGenCoreLibLog 2>&1
    local exit_code="$?"
    if [[ "$exit_code" != 0 ]]; then
        echo "${__ErrMsgPrefix}Failed to generate native image for System.Private.CoreLib. Refer to $__CrossGenCoreLibLog"
        exit "$exit_code"
    fi

    if [[ "$__BuildOS" == "Linux" ]]; then
        echo "Generating symbol file for System.Private.CoreLib.dll"
        echo "$__CrossGenExec /Platform_Assemblies_Paths $__BinDir /CreatePerfMap $__BinDir $__BinDir/System.Private.CoreLib.dll"
        "$__CrossGenExec" /nologo /Platform_Assemblies_Paths $__BinDir /CreatePerfMap $__BinDir $__BinDir/System.Private.CoreLib.dll >> $__CrossGenCoreLibLog 2>&1
        local exit_code="$?"
        if [[ "$exit_code" != 0 ]]; then
            echo "${__ErrMsgPrefix}Failed to generate symbol file for System.Private.CoreLib. Refer to $__CrossGenCoreLibLog"
            exit "$exit_code"
        fi
    fi
}

handle_arguments_local() {
    case "$1" in
        crossgenonly|-crossgenonly)
            __SkipNative=1
            ;;

        disableoss|-disableoss)
            __SignTypeArg="/p:SignType=real"
            ;;

        ibcinstrument|-ibcinstrument)
            __IbcTuning="/Tuning"
            ;;

        ignorewarnings|-ignorewarnings)
            __IgnoreWarnings=1
            __CMakeArgs="-DCLR_CMAKE_WARNINGS_ARE_ERRORS=OFF $__CMakeArgs"
            ;;

        nopgooptimize|-nopgooptimize)
            __PgoOptimize=0
            __SkipRestoreOptData=1
            ;;

        officialbuildid=*|-officialbuildid=*)
            __Id=$(echo "$1" | cut -d'=' -f 2)
            __OfficialBuildIdArg="/p:OfficialBuildId=$__Id"
            ;;

        partialngen|-partialngen)
            __PartialNgen=1
            ;;

        pgoinstrument|-pgoinstrument)
            __PgoInstrument=1
            ;;

        skipcrossarchnative|-skipcrossarchnative)
            __SkipCrossArchNative=1
            ;;

        skipcrossgen|-skipcrossgen)
            __SkipMSCorLib=1
            ;;

        skipmscorlib|-skipmscorlib)
            __SkipMSCorLib=1
            ;;

        skiprestore|-skiprestore)
            __SkipRestoreArg="/p:RestoreDuringBuild=false"
            ;;

        staticanalyzer|-staticanalyzer)
            __StaticAnalyzer=1
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
__IgnoreWarnings=0

# Set the various build properties here so that CMake and MSBuild can pick them up
__Compiler=clang
__CompilerMajorVersion=
__CompilerMinorVersion=
__CommonMSBuildArgs=
__ConfigureOnly=0
__CrossBuild=0
__CrossgenOnly=0
__DistroRid=""
__IbcOptDataPath=""
__IbcTuning=""
__IsMSBuildOnNETCoreSupported=0
__MSBCleanBuildArgs=
__OfficialBuildIdArg=""
__PartialNgen=0
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
__SkipMSCorLib=0
__SkipGenerateVersion=0
__SkipMSCorLib=0
__SkipManaged=0
__SkipRestore=""
__SkipRestoreArg="/p:RestoreDuringBuild=true"
__SkipRestoreOptData=0
__SourceDir="$__ProjectDir/src"
__StaticAnalyzer=0
__UnprocessedBuildArgs=
__UseNinja=0
__VerboseBuild=0
__ValidateCrossArg=1
__CMakeArgs=""

source "$__ProjectRoot"/_build-commons.sh

if [[ "${__BuildArch}" != "${__HostArch}" ]]; then
    __CrossBuild=1
fi

# Set dependent variables
__LogsDir="$__RootBinDir/log"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/bin/coreclr/$__BuildOS.$__BuildArch.$__BuildType"
__IntermediatesDir="$__RootBinDir/obj/coreclr/$__BuildOS.$__BuildArch.$__BuildType"
__ArtifactsIntermediatesDir="$__RepoRootDir/artifacts/obj/coreclr"
export __IntermediatesDir __ArtifactsIntermediatesDir
__CrossComponentBinDir="$__BinDir"

__CrossArch="$__HostArch"
if [[ "$__CrossBuild" == 1 ]]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossGenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__BuildOS.$__BuildArch.$__BuildType.log"

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
__CMakeArgs="-DCLR_CMAKE_PGO_INSTRUMENT=$__PgoInstrument -DCLR_CMAKE_OPTDATA_PATH=$__PgoOptDataPath -DCLR_CMAKE_PGO_OPTIMIZE=$__PgoOptimize -DCLR_REPO_ROOT_DIR=\"$__RepoRootDir\" $__CMakeArgs"

if [[ "$__SkipConfigure" == 0 && "$__CodeCoverage" == 1 ]]; then
    __CMakeArgs="-DCLR_CMAKE_ENABLE_CODE_COVERAGE=1 $__CMakeArgs"
fi

if [[ "$__SkipNative" == 1 ]]; then
    echo "Skipping CoreCLR component build."
else
    build_native "$__BuildArch" "$__ProjectRoot" "$__ProjectRoot" "$__IntermediatesDir" "CoreCLR component"

    # Build cross-architecture components
    if [[ "$__SkipCrossArchNative" != 1 ]]; then
        if [[ "$__CrossBuild" == 1 ]]; then
            build_cross_architecture_components
        fi
    fi
fi


# Crossgen System.Private.CoreLib

__CoreLibILDir=$__BinDir/IL

if [[ "$__SkipMSCorLib" != 1 ]]; then
    # The cross build generates a crossgen with the target architecture.
    if [[ "$__CrossBuild" == 0 ]]; then
        # The architecture of host pc must be same architecture with target.
        if [[ "$__HostArch" == "$__BuildArch" ]]; then
            build_CoreLib_ni "$__BinDir/crossgen" "$__CoreLibILDir"
        elif [[ ( "$__HostArch" == "x64" ) && ( "$__BuildArch" == "x86" ) ]]; then
            build_CoreLib_ni "$__BinDir/crossgen" "$__CoreLibILDir"
        elif [[ ( "$__HostArch" == "arm64" ) && ( "$__BuildArch" == "arm" ) ]]; then
            build_CoreLib_ni "$__BinDir/crossgen" "$__CoreLibILDir"
        else
            exit 1
        fi
    else
        if [[ ( "$__CrossArch" == "x86" ) && ( "$__BuildArch" == "arm" ) ]]; then
            build_CoreLib_ni "$__CrossComponentBinDir/crossgen" "$__CoreLibILDir"
        elif [[ ( "$__CrossArch" == "x64" ) && ( "$__BuildArch" == "arm" ) ]]; then
            build_CoreLib_ni "$__CrossComponentBinDir/crossgen" "$__CoreLibILDir"
        elif [[ ( "$__HostArch" == "x64" ) && ( "$__BuildArch" == "arm64" ) ]]; then
            build_CoreLib_ni "$__CrossComponentBinDir/crossgen" "$__CoreLibILDir"
        else
            # Crossgen not performed, so treat the IL version as the final version
            cp "$__CoreLibILDir"/System.Private.CoreLib.dll "$__BinDir"/System.Private.CoreLib.dll
        fi
    fi
fi


# Build complete

echo "Repo successfully built."
echo "Product binaries are available at $__BinDir"
exit 0
