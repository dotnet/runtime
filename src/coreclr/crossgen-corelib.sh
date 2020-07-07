#!/usr/bin/env bash

usage_list+=("-ibcinstrument: generate IBC-tuning-enabled native images when invoking crossgen.")
usage_list+=("-partialngen: build CoreLib as PartialNGen.")
usage_list+=("-pgoinstrument: generate instrumented code for profile guided optimization enabled binaries.")

setup_dirs_local()
{
    setup_dirs

    mkdir -p "$__LogsDir"
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

handle_arguments_local() {
    case "$1" in

        ibcinstrument|-ibcinstrument)
            __IbcTuning="/Tuning"
            ;;

        partialngen|-partialngen)
            __PartialNgen=1
            ;;

        pgoinstrument|-pgoinstrument)
            __PgoInstrument=1
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac
}

echo "Commencing Crossgenning System.Private.CoreLib"

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

__CrossBuild=0
__IbcTuning=""
__PartialNgen=0
__PgoInstrument=0
__RootBinDir="$__RepoRootDir/artifacts"
__SkipMSCorLib=0
__SkipRestore=""
__StaticAnalyzer=0
__UnprocessedBuildArgs=

source "$__ProjectRoot"/_build-commons.sh

if [[ "${__BuildArch}" != "${__HostArch}" ]] || [[ "$__BuildOS" != "$__TargetOS" ]]; then
    __CrossBuild=1
fi

# Set dependent variables
__LogsDir="$__RootBinDir/log/$__BuildType"

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/bin/coreclr/$__TargetOS.$__BuildArch.$__BuildType"
__IntermediatesDir="$__RootBinDir/obj/coreclr/$__TargetOS.$__BuildArch.$__BuildType"
__CrossComponentBinDir="$__BinDir"

__CrossArch="$__HostArch"
if [[ "$__CrossBuild" == 1 ]]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossGenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__TargetOS.$__BuildArch.$__BuildType.log"

# Crossgen System.Private.CoreLib

__CoreLibILDir=$__BinDir/IL

setup_dirs_local

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


# Build complete

echo "Native System.Private.CoreLib generated."
echo "Product binaries are available at $__BinDir"
exit 0
