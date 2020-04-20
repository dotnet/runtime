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
usage_list+=("-skipcrossarchnative: Disable Open Source Signing for System.Private.CoreLib.")
usage_list+=("-skipmanagedtools: generate instrumented code for profile guided optimization enabled binaries.")
usage_list+=("-skipmscorlib: generate IBC-tuning-enabled native images when invoking crossgen.")
usage_list+=("-skipnuget: skip NuGet package generation.")
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
                                               -bl:"$__LogsDir/OptRestore_$__ConfigTriplet.binlog"\
                                               $__CommonMSBuildArgs $__UnprocessedBuildArgs \
                                               /nodereuse:false
        local exit_code="$?"
        if [[ "$exit_code" != 0 ]]; then
            echo "${__ErrMsgPrefix}Failed to restore the optimization data package."
            exit "$exit_code"
        fi
    fi

    if [[ "$__PgoOptimize" == 1 && "$__IsMSBuildOnNETCoreSupported" == 1 ]]; then
        # Parse the optdata package versions out of msbuild so that we can pass them on to CMake

        local PgoDataPackagePathOutputFile="${__IntermediatesDir}/optdatapath.txt"

        # Writes into ${PgoDataPackagePathOutputFile}
        "$__RepoRootDir/eng/common/msbuild.sh" /clp:nosummary $__ArcadeScriptArgs $OptDataProjectFilePath /t:DumpPgoDataPackagePath\
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

generate_event_logging_sources()
{
    __OutputEventingDir="$1"

    __PythonWarningFlags="-Wall"
    if [[ "$__IgnoreWarnings" == 0 ]]; then
        __PythonWarningFlags="$__PythonWarningFlags -Werror"
    fi

    echo "Laying out dynamically generated EventSource classes"
    "$PYTHON" -B $__PythonWarningFlags "$__ProjectRoot/src/scripts/genRuntimeEventSources.py" --man "$__ProjectRoot/src/vm/ClrEtwAll.man" --intermediate "$__OutputEventingDir"
}

generate_event_logging()
{
    # Event Logging Infrastructure
    if [[ "$__SkipMSCorLib" == 0 ]]; then
        generate_event_logging_sources "$__ArtifactsIntermediatesDir/Eventing/$__BuildArch/$__BuildType"
    fi
}

build_cross_architecture_components()
{
    local intermediatesForBuild="$__IntermediatesDir/Host$__CrossArch/crossgen"
    local crossArchBinDir="$__BinDir/$__CrossArch"

    mkdir -p "$intermediatesForBuild"
    mkdir -p "$crossArchBinDir"

    generate_event_logging_sources "$intermediatesForBuild" "the crossarch build system"

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
    echo "Generating native image of System.Private.CoreLib.dll for $__TargetOS.$__BuildArch.$__BuildType. Logging to \"$__CrossGenCoreLibLog\"."
    echo "$__CrossGenExec /Platform_Assemblies_Paths $__CoreLibILDir $__IbcTuning /out $__BinDir/System.Private.CoreLib.dll $__CoreLibILDir/System.Private.CoreLib.dll"
    "$__CrossGenExec" /nologo /Platform_Assemblies_Paths $__CoreLibILDir $__IbcTuning /out $__BinDir/System.Private.CoreLib.dll $__CoreLibILDir/System.Private.CoreLib.dll >> $__CrossGenCoreLibLog 2>&1
    local exit_code="$?"
    if [[ "$exit_code" != 0 ]]; then
        echo "${__ErrMsgPrefix}Failed to generate native image for System.Private.CoreLib. Refer to $__CrossGenCoreLibLog"
        exit "$exit_code"
    fi

    if [[ "$__TargetOS" == "Linux" ]]; then
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

build_CoreLib()
{
    if [[ "$__IsMSBuildOnNETCoreSupported" == 0 ]]; then
        echo "System.Private.CoreLib.dll build unsupported."
        return
    fi

    if [[ "$__SkipMSCorLib" == 1 ]]; then
       echo "Skipping building System.Private.CoreLib."
       return
    fi

    echo "Commencing build of managed components for $__TargetOS.$__BuildArch.$__BuildType"

    # Invoke MSBuild
    __ExtraBuildArgs=""

    if [[ "$__BuildManagedTools" -eq "1" ]]; then
        __ExtraBuildArgs="$__ExtraBuildArgs /p:BuildManagedTools=true"
    fi

    "$__RepoRootDir/eng/common/msbuild.sh" /clp:nosummary $__ArcadeScriptArgs \
                                           $__ProjectDir/src/build.proj /t:Restore \
                                           /p:PortableBuild=true /maxcpucount /p:IncludeRestoreOnlyProjects=true \
                                           /flp:Verbosity=normal\;LogFile=$__LogsDir/System.Private.CoreLib_$__ConfigTriplet.log \
                                           -bl:"$__LogsDir/System.Private.CoreLib_$__ConfigTriplet.binlog" \
                                           /p:__IntermediatesDir=$__IntermediatesDir /p:__RootBinDir=$__RootBinDir \
                                           $__CommonMSBuildArgs $__ExtraBuildArgs $__UnprocessedBuildArgs

    local exit_code="$?"
    if [[ "$exit_code" != 0 ]]; then
        echo "${__ErrMsgPrefix}Failed to restore managed components."
        exit "$exit_code"
    fi

    "$__RepoRootDir/eng/common/msbuild.sh" /clp:nosummary $__ArcadeScriptArgs \
                                           $__ProjectDir/src/build.proj \
                                           /p:PortableBuild=true /maxcpucount \
                                           /flp:Verbosity=normal\;LogFile=$__LogsDir/System.Private.CoreLib_$__TargetOS__$__BuildArch__$__BuildType.log \
                                           -bl:"$__LogsDir/System.Private.CoreLib_$__ConfigTriplet.binlog" \
                                           /p:__IntermediatesDir=$__IntermediatesDir /p:__RootBinDir=$__RootBinDir \
                                           $__CommonMSBuildArgs $__ExtraBuildArgs $__UnprocessedBuildArgs

    local exit_code="$?"
        if [[ "$exit_code" != 0 ]]; then
        echo "${__ErrMsgPrefix}Failed to build managed components."
        exit "$exit_code"
    fi

    if [[ "$__BuildManagedTools" -eq "1" ]]; then
        echo "Publishing crossgen2 for $__DistroRid"
        "$__RepoRootDir/dotnet.sh" publish --self-contained -r $__DistroRid -c $__BuildType -o "$__BinDir/crossgen2" "$__ProjectRoot/src/tools/crossgen2/crossgen2/crossgen2.csproj" /nologo /p:TargetArchitecture=$__BuildArch

        local exit_code="$?"
        if [[ "$exit_code" != 0 ]]; then
            echo "${__ErrMsgPrefix}Failed to build crossgen2."
            exit "$exit_code"
        fi

        if [[ "$__HostOS" == "OSX" ]]; then
            cp "$__BinDir/libclrjit.dylib" "$__BinDir/crossgen2/libclrjitilc.dylib"
            cp "$__BinDir/libjitinterface.dylib" "$__BinDir/crossgen2/libjitinterface.dylib"
        else
            cp "$__BinDir/libclrjit.so" "$__BinDir/crossgen2/libclrjitilc.so"
            cp "$__BinDir/libjitinterface.so" "$__BinDir/crossgen2/libjitinterface.so"
        fi
    fi

    local __CoreLibILDir="$__BinDir"/IL

    if [[ "$__SkipCrossgen" == 1 ]]; then
        echo "Skipping generating native image"

        if [[ "$__CrossBuild" == 1 ]]; then
            # Crossgen not performed, so treat the IL version as the final version
            cp "$__CoreLibILDir"/System.Private.CoreLib.dll "$__BinDir"/System.Private.CoreLib.dll
        fi

        return
    fi

    # The cross build generates a crossgen with the target architecture.
    if [[ "$__CrossBuild" == 0 ]]; then
       if [[ "$__SkipCoreCLR" == 1 ]]; then
           return
       fi

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
}

generate_NugetPackages()
{
    # We can only generate nuget package if we also support building mscorlib as part of this build.
    if [[ "$__IsMSBuildOnNETCoreSupported" == 0 ]]; then
        echo "Nuget package generation unsupported."
        return
    fi

    # Since we can build mscorlib for this OS, did we build the native components as well?
    if [[ "$__SkipCoreCLR" == 1 && "$__CrossgenOnly" == 0 ]]; then
        echo "Unable to generate nuget packages since native components were not built."
        return
    fi

    echo "Generating nuget packages for $__TargetOS"
    echo "DistroRid is $__DistroRid"
    echo "ROOTFS_DIR is $ROOTFS_DIR"
    # Build the packages
    # Package build uses the Arcade system and scripts, relying on it to restore required toolsets as part of build
    "$__RepoRootDir"/eng/common/build.sh -r -b -projects "$__SourceDir"/.nuget/coreclr-packages.proj \
                                       -verbosity minimal -bl:"$__LogsDir/Nuget_$__TargetOS__$__BuildArch__$__BuildType.binlog" \
                                       /p:PortableBuild=true \
                                       /p:"__IntermediatesDir=$__IntermediatesDir" /p:"__RootBinDir=$__RootBinDir" /p:"__DoCrossArchBuild=$__CrossBuild" \
                                       $__CommonMSBuildArgs $__UnprocessedBuildArgs

    local exit_code="$?"
    if [[ "$exit_code" != 0 ]]; then
        echo "${__ErrMsgPrefix}Failed to generate Nuget packages."
        exit "$exit_code"
    fi
}

handle_arguments_local() {
    case "$1" in
        crossgenonly|-crossgenonly)
            __SkipMSCorLib=1
            __SkipCoreCLR=1
            __CrossgenOnly=1
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

        skipcoreclr|-skipcoreclr)
            # Accept "skipcoreclr" for backwards-compatibility.
            __SkipCoreCLR=1
            ;;

        skipcrossarchnative|-skipcrossarchnative)
            __SkipCrossArchNative=1
            ;;

        skipcrossgen|-skipcrossgen)
            __SkipCrossgen=1
            ;;

        skipmanagedtools|-skipmanagedtools)
            __BuildManagedTools=0
            ;;

        skipmscorlib|-skipmscorlib)
            __SkipMSCorLib=1
            ;;

        skipnuget|-skipnuget|skipbuildpackages|-skipbuildpackages)
            __SkipNuget=1
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
echo "WARNING: This build script is deprecated and will be deleted soon. Use the root build script to build CoreCLR. If you want to build the CoreCLR runtime without using MSBuild, use the build-native.sh script."
echo "See https://github.com/dotnet/runtime/issues/32991 for more information."

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
__BuildManagedTools=1
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
__SkipCoreCLR=0
__SkipCrossArchNative=0
__SkipCrossgen=0
__SkipGenerateVersion=0
__SkipMSCorLib=0
__SkipManaged=0
__SkipNuget=0
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
__LogsDir="$__RootBinDir/log/$__BuildType"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"
__ConfigTriplet=$__TargetOS__$__BuildArch__$__BuildType

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/bin/coreclr/$__TargetOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__IntermediatesDir="$__RootBinDir/obj/coreclr/$__TargetOS.$__BuildArch.$__BuildType"
__ArtifactsIntermediatesDir="$__RepoRootDir/artifacts/obj/coreclr"
export __IntermediatesDir __ArtifactsIntermediatesDir

__CrossComponentBinDir="$__BinDir"

__CrossArch="$__HostArch"
if [[ "$__CrossBuild" == 1 ]]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossGenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__ConfigTriplet.log"

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

# Generate event logging infrastructure sources
generate_event_logging

# Build the coreclr (native) components.
__CMakeArgs="-DCLR_CMAKE_PGO_INSTRUMENT=$__PgoInstrument -DCLR_CMAKE_OPTDATA_PATH=$__PgoOptDataPath -DCLR_CMAKE_PGO_OPTIMIZE=$__PgoOptimize -DCLR_REPO_ROOT_DIR=\"$__RepoRootDir\" $__CMakeArgs"

if [[ "$__SkipConfigure" == 0 && "$__CodeCoverage" == 1 ]]; then
    __CMakeArgs="-DCLR_CMAKE_ENABLE_CODE_COVERAGE=1 $__CMakeArgs"
fi

if [[ "$__SkipCoreCLR" == 1 ]]; then
    echo "Skipping CoreCLR component build."
else
    build_native "$__BuildArch" "$__ProjectRoot" "$__ProjectRoot" "$__IntermediatesDir" "CoreCLR component"
fi

# Build cross-architecture components
if [[ "$__SkipCrossArchNative" != 1 ]]; then
    if [[ "$__CrossBuild" == 1 ]]; then
        build_cross_architecture_components
    fi
fi

# Build System.Private.CoreLib.

build_CoreLib

if [[ "$__CrossgenOnly" == 1 ]]; then
    build_CoreLib_ni "$__BinDir/crossgen"
fi

# Generate nuget packages
if [[ "$__SkipNuget" != 1 ]]; then
    generate_NugetPackages
fi


# Build complete

echo "Repo successfully built."
echo "Product binaries are available at $__BinDir"
echo "WARNING: This build script is deprecated and will be deleted soon. Use the root build script to build CoreCLR. If you want to build the CoreCLR runtime without using MSBuild, use the build-native.sh script."
echo "See https://github.com/dotnet/runtime/issues/32991 for more information."
exit 0
