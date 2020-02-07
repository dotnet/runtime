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

        ibcinstrument|-ibcinstrument)
            __IbcTuning="/Tuning"
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

if [[ "${__BuildArch}" != "${__HostArch}" ]]; then
    __CrossBuild=1
fi

# Set dependent variables
__LogsDir="$__RootBinDir/log"

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/bin/coreclr/$__BuildOS.$__BuildArch.$__BuildType"
__CrossComponentBinDir="$__BinDir"

__CrossArch="$__HostArch"
if [[ "$__CrossBuild" == 1 ]]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossGenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__BuildOS.$__BuildArch.$__BuildType.log"

# Crossgen System.Private.CoreLib

__CoreLibILDir=$__BinDir/IL

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
