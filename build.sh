#!/usr/bin/env bash

# Work around Jenkins CI + msbuild problem: Jenkins sometimes creates very large environment
# variables, and msbuild can't handle environment blocks with such large variables. So clear
# out the variables that might be too large.
export ghprbCommentBody=

# resolve python-version to use
if [ "$PYTHON" == "" ] ; then
    if ! PYTHON=$(command -v python3 || command -v python2 || command -v python || command -v py)
    then
       echo "Unable to locate build-dependency python!" 1>&2
       exit 1
    fi
fi
# validate python-dependency
# useful in case of explicitly set option.
if ! command -v $PYTHON > /dev/null
then
   echo "Unable to locate build-dependency python ($PYTHON)!" 1>&2
   exit 1
fi

export PYTHON

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [-verbose] [-coverage] [-cross] [-gccx.y] [-clangx.y] [-ninja] [-configureonly] [-skipconfigure] [-skipnative] [-skipcrossarchnative] [-skipmanaged] [-skipmscorlib] [-skiptests] [-stripsymbols] [-ignorewarnings] [-cmakeargs] [-bindir]"
    echo "BuildArch can be: -x64, -x86, -arm, -armel, -arm64"
    echo "BuildType can be: -debug, -checked, -release"
    echo "-coverage - optional argument to enable code coverage build (currently supported only for Linux and OSX)."
    echo "-ninja - target ninja instead of GNU make"
    echo "-gccx.y - optional argument to build using gcc version x.y."
    echo "-clangx.y - optional argument to build using clang version x.y."
    echo "-cross - optional argument to signify cross compilation,"
    echo "       - will use ROOTFS_DIR environment variable if set."
    echo "-nopgooptimize - do not use profile guided optimizations."
    echo "-pgoinstrument - generate instrumented code for profile guided optimization enabled binaries."
    echo "-ibcinstrument - generate IBC-tuning-enabled native images when invoking crossgen."
    echo "-configureonly - do not perform any builds; just configure the build."
    echo "-skipconfigure - skip build configuration."
    echo "-skipnative - do not build native components."
    echo "-skipcrossarchnative - do not build cross-architecture native components."
    echo "-skipmanaged - do not build managed components."
    echo "-skipmscorlib - do not build mscorlib.dll."
    echo "-skiptests - skip the tests in the 'tests' subdirectory."
    echo "-skipnuget - skip building nuget packages."
    echo "-skiprestoreoptdata - skip restoring optimization data used by profile-based optimizations."
    echo "-skipcrossgen - skip native image generation"
    echo "-crossgenonly - only run native image generation"
    echo "-partialngen - build CoreLib as PartialNGen"
    echo "-verbose - optional argument to enable verbose build output."
    echo "-skiprestore: skip restoring packages ^(default: packages are restored during build^)."
    echo "-disableoss: Disable Open Source Signing for System.Private.CoreLib."
    echo "-officialbuildid=^<ID^>: specify the official build ID to be used by this build."
    echo "-stripSymbols - Optional argument to strip native symbols during the build."
    echo "-skipgenerateversion - disable version generation even if MSBuild is supported."
    echo "-ignorewarnings - do not treat warnings as errors"
    echo "-cmakeargs - user-settable additional arguments passed to CMake."
    echo "-bindir - output directory (defaults to $__ProjectRoot/bin)"
    echo "-msbuildonunsupportedplatform - build managed binaries even if distro is not officially supported."
    echo "-numproc - set the number of build processes."
    echo "-portablebuild - pass -portablebuild=false to force a non-portable build."
    echo "-staticanalyzer - build with clang static analyzer enabled."
    exit 1
}

initTargetDistroRid()
{
    source init-distro-rid.sh

    local passedRootfsDir=""

    # Only pass ROOTFS_DIR if cross is specified.
    if (( ${__CrossBuild} == 1 )); then
        passedRootfsDir=${ROOTFS_DIR}
    elif [ "${__BuildArch}" != "${__HostArch}" ]; then
        echo "Error, you are building a cross scenario without passing -cross."
        exit 1
    fi

    initDistroRidGlobal ${__BuildOS} ${__BuildArch} ${__PortableBuild} ${passedRootfsDir}
}

setup_dirs()
{
    echo Setting up directories for build

    mkdir -p "$__RootBinDir"
    mkdir -p "$__BinDir"
    mkdir -p "$__LogsDir"
    mkdir -p "$__MsbuildDebugLogsDir"
    mkdir -p "$__IntermediatesDir"

    if [ $__CrossBuild == 1 ]; then
        mkdir -p "$__CrossComponentBinDir"
    fi
}

# Check the system to ensure the right prereqs are in place

check_prereqs()
{
    echo "Checking prerequisites..."

    # Check presence of CMake on the path
    hash cmake 2>/dev/null || { echo >&2 "Please install cmake before running this script"; exit 1; }


    # Minimum required version of clang is version 4.0 for arm/armel cross build
    if [[ $__CrossBuild == 1 && $__GccBuild == 0 &&  ("$__BuildArch" == "arm" || "$__BuildArch" == "armel") ]]; then
        if ! [[ "$__ClangMajorVersion" -ge "4" ]]; then
            echo "Please install clang4.0 or latest for arm/armel cross build"; exit 1;
        fi
    fi

    # Check for clang
    if [[ $__GccBuild == 0 ]]; then
        __ClangCombinedDottedVersion=$__ClangMajorVersion;
        if [[ "$__ClangMinorVersion" != "" ]]; then
            __ClangCombinedDottedVersion=$__ClangCombinedDottedVersion.$__ClangMinorVersion
        fi
        hash clang-$__ClangCombinedDottedVersion 2>/dev/null ||  hash clang$__ClangMajorVersion$__ClangMinorVersion 2>/dev/null || hash clang 2>/dev/null || { echo >&2 "Please install clang-$__ClangMajorVersion.$__ClangMinorVersion before running this script"; exit 1; }
    else
        __GccCombinedDottedVersion=$__GccMajorVersion;
        if [[ "$__GccMinorVersion" != "" ]]; then
            __GccCombinedDottedVersion=$__GccCombinedDottedVersion.$__GccMinorVersion
        fi
        hash gcc-$__GccCombinedDottedVersion 2>/dev/null ||  hash gcc$__GccMajorVersion$__GccMinorVersion 2>/dev/null || hash gcc 2>/dev/null || { echo >&2 "Please install gcc-$__GccMajorVersion.$__GccMinorVersion before running this script"; exit 1; }
    fi

}

restore_optdata()
{
    # we only need optdata on a Release build
    if [[ "$__BuildType" != "Release" ]]; then __SkipRestoreOptData=1; fi

    if [[ ( $__SkipRestoreOptData == 0 ) && ( $__isMSBuildOnNETCoreSupported == 1 ) ]]; then
        echo "Restoring the OptimizationData package"
        "$__ProjectRoot/dotnet.sh" msbuild /nologo /verbosity:minimal /clp:Summary \
                                   /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true \
                                   /p:UsePartialNGENOptimization=false /maxcpucount \
                                   /t:RestoreOptData ./build.proj \
                                   $__CommonMSBuildArgs $__UnprocessedBuildArgs
        if [ $? != 0 ]; then
            echo "Failed to restore the optimization data package."
            exit 1
        fi
    fi

    if [ $__isMSBuildOnNETCoreSupported == 1 ]; then
        # Parse the optdata package versions out of msbuild so that we can pass them on to CMake
        local DotNetCli="$__ProjectRoot/.dotnet/dotnet"
        if [ ! -f $DotNetCli ]; then
            source "$__ProjectRoot/init-tools.sh"
            if [ $? != 0 ]; then
                echo "Failed to restore buildtools."
                exit 1
            fi
        fi
        local OptDataProjectFilePath="$__ProjectRoot/src/.nuget/optdata/optdata.csproj"
        __PgoOptDataVersion=$(DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 $DotNetCli msbuild $OptDataProjectFilePath /t:DumpPgoDataPackageVersion /nologo | sed 's/^\s*//')
        __IbcOptDataVersion=$(DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 $DotNetCli msbuild $OptDataProjectFilePath /t:DumpIbcDataPackageVersion /nologo | sed 's/^[[:blank:]]*//')
    fi
}

generate_event_logging_sources()
{
    __OutputDir=$1
    __ConsumingBuildSystem=$2

    __OutputIncDir="$__OutputDir/src/inc"
    __OutputEventingDir="$__OutputDir/Eventing"
    __OutputEventProviderDir="$__OutputEventingDir/eventprovider"

    echo "Laying out dynamically generated files consumed by $__ConsumingBuildSystem"
    echo "Laying out dynamically generated Event test files, etmdummy stub functions, and external linkages"

    __PythonWarningFlags="-Wall"
    if [[ $__IgnoreWarnings == 0 ]]; then
        __PythonWarningFlags="$__PythonWarningFlags -Werror"
    fi

    $PYTHON -B $__PythonWarningFlags "$__ProjectRoot/src/scripts/genEventing.py" --inc $__OutputIncDir --dummy $__OutputIncDir/etmdummy.h --man "$__ProjectRoot/src/vm/ClrEtwAll.man" --testdir "$__OutputEventProviderDir/tests"
    if [[ $? != 0 ]]; then
        exit 1
    fi

    echo "Laying out dynamically generated EventPipe Implementation"
    $PYTHON -B $__PythonWarningFlags "$__ProjectRoot/src/scripts/genEventPipe.py" --man "$__ProjectRoot/src/vm/ClrEtwAll.man" --exc "$__ProjectRoot/src/vm/ClrEtwAllMeta.lst" --intermediate "$__OutputEventingDir/eventpipe"

    echo "Laying out dynamically generated EventSource classes"
    $PYTHON -B $__PythonWarningFlags "$__ProjectRoot/src/scripts/genRuntimeEventSources.py" --man "$__ProjectRoot/src/vm/ClrEtwAll.man" --intermediate "$__OutputEventingDir"

    # determine the logging system
    case $__BuildOS in
        Linux|FreeBSD)
            echo "Laying out dynamically generated Event Logging Implementation of Lttng"
            $PYTHON -B $__PythonWarningFlags "$__ProjectRoot/src/scripts/genLttngProvider.py" --man "$__ProjectRoot/src/vm/ClrEtwAll.man" --intermediate "$__OutputEventProviderDir"
            if [[ $? != 0 ]]; then
                exit 1
            fi
            ;;
        *)
            echo "Laying out dummy event logging provider"
            $PYTHON -B $__PythonWarningFlags "$__ProjectRoot/src/scripts/genDummyProvider.py" --man "$__ProjectRoot/src/vm/ClrEtwAll.man" --intermediate "$__OutputEventProviderDir"
            if [[ $? != 0 ]]; then
                exit 1
            fi
            ;;
    esac
}

generate_event_logging()
{
    # Event Logging Infrastructure
    if [[ $__SkipCoreCLR == 0 || $__SkipMSCorLib == 0 || $__ConfigureOnly == 1 ]]; then
        generate_event_logging_sources "$__IntermediatesDir" "the native build system"
    fi
}

build_native()
{
    skipCondition=$1
    platformArch="$2"
    intermediatesForBuild="$3"
    extraCmakeArguments="$4"
    message="$5"

    if [ $skipCondition == 1 ]; then
        echo "Skipping $message build."
        return
    fi

    # All set to commence the build
    echo "Commencing build of $message for $__BuildOS.$__BuildArch.$__BuildType in $intermediatesForBuild"

    generator=""
    buildFile="Makefile"
    buildTool="make"
    if [ $__UseNinja == 1 ]; then
        generator="ninja"
        buildFile="build.ninja"
        if ! buildTool=$(command -v ninja || command -v ninja-build); then
           echo "Unable to locate ninja!" 1>&2
           exit 1
        fi
    fi

    if [ $__SkipConfigure == 0 ]; then
        # if msbuild is not supported, then set __SkipGenerateVersion to 1
        if [ $__isMSBuildOnNETCoreSupported == 0 ]; then __SkipGenerateVersion=1; fi
        # Drop version.c file
        __versionSourceFile="$intermediatesForBuild/version.c"
        if [ $__SkipGenerateVersion == 0 ]; then
            pwd
            "$__ProjectRoot/dotnet.sh" msbuild /nologo /verbosity:minimal /clp:Summary \
                                       /l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll\;LogFile=binclash.log \
                                       /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true \
                                       /p:UsePartialNGENOptimization=false /maxcpucount \
                                       "$__ProjectDir/build.proj" /p:GenerateVersionSourceFile=true /t:GenerateVersionSourceFile /p:NativeVersionSourceFile=$__versionSourceFile \
                                       $__CommonMSBuildArgs $__UnprocessedBuildArgs
        else
            # Generate the dummy version.c, but only if it didn't exist to make sure we don't trigger unnecessary rebuild
            __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
            if [ -e $__versionSourceFile ]; then
                read existingVersionSourceLine < $__versionSourceFile
            fi
            if [ "$__versionSourceLine" != "$existingVersionSourceLine" ]; then
                echo $__versionSourceLine > $__versionSourceFile
            fi
        fi


        pushd "$intermediatesForBuild"
        # Regenerate the CMake solution

        scriptDir="$__ProjectRoot/src/pal/tools"
        if [[ $__GccBuild == 0 ]]; then
            scan_build=
            if [[ $__StaticAnalyzer == 1 ]]; then
                scan_build=scan-build
            fi
            echo "Invoking \"$scriptDir/gen-buildsys-clang.sh\" \"$__ProjectRoot\" $__ClangMajorVersion \"$__ClangMinorVersion\" $platformArch "$scriptDir" $__BuildType $__CodeCoverage $scan_build $generator $extraCmakeArguments $__cmakeargs"
            source "$scriptDir/gen-buildsys-clang.sh" "$__ProjectRoot" $__ClangMajorVersion "$__ClangMinorVersion" $platformArch "$scriptDir" $__BuildType $__CodeCoverage $scan_build $generator "$extraCmakeArguments" "$__cmakeargs"
        else
            echo "Invoking \"$scriptDir/gen-buildsys-gcc.sh\" \"$__ProjectRoot\" $__GccMajorVersion \"$__GccMinorVersion\" $platformArch "$scriptDir" $__BuildType $__CodeCoverage $generator $extraCmakeArguments $__cmakeargs"
            source "$scriptDir/gen-buildsys-gcc.sh" "$__ProjectRoot" "$__GccMajorVersion" "$__CGccMinorVersion" $platformArch "$scriptDir" $__BuildType $__CodeCoverage $generator "$extraCmakeArguments" "$__cmakeargs"
        fi
        popd
    fi

    if [ ! -f "$intermediatesForBuild/$buildFile" ]; then
        echo "Failed to generate $message build project!"
        exit 1
    fi

    # Build
    if [ $__ConfigureOnly == 1 ]; then
        echo "Finish configuration & skipping $message build."
        return
    fi

    # Check that the makefiles were created.
    pushd "$intermediatesForBuild"

    if [ $__StaticAnalyzer == 1 ]; then
        buildTool="$SCAN_BUILD_COMMAND $buildTool"
    fi

    echo "Executing $buildTool install -j $__NumProc"

    $buildTool install -j $__NumProc
    if [ $? != 0 ]; then
        echo "Failed to build $message."
        exit 1
    fi

    popd
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

    export __CMakeBinDir="$crossArchBinDir"
    export CROSSCOMPILE=0

    __ExtraCmakeArgs="-DCLR_CMAKE_TARGET_ARCH=$__BuildArch -DCLR_CMAKE_TARGET_OS=$__BuildOS -DCLR_CMAKE_PACKAGES_DIR=$__PackagesDir -DCLR_CMAKE_PGO_INSTRUMENT=$__PgoInstrument -DCLR_CMAKE_OPTDATA_VERSION=$__PgoOptDataVersion -DCLR_CMAKE_PGO_OPTIMIZE=$__PgoOptimize -DCLR_CROSS_COMPONENTS_BUILD=1"
    build_native $__SkipCrossArchBuild "$__CrossArch" "$intermediatesForBuild" "$__ExtraCmakeArgs" "cross-architecture components"

    export CROSSCOMPILE=1
}

isMSBuildOnNETCoreSupported()
{
    __isMSBuildOnNETCoreSupported=$__msbuildonunsupportedplatform

    if [ $__isMSBuildOnNETCoreSupported == 1 ]; then
        return
    fi

    if [ $__SkipManaged == 1 ]; then
        __isMSBuildOnNETCoreSupported=0
        return
    fi

    if [ "$__HostArch" == "x64" ]; then
        if [ "$__HostOS" == "Linux" ]; then
            __isMSBuildOnNETCoreSupported=1
            # note: the RIDs below can use globbing patterns
            UNSUPPORTED_RIDS=("ubuntu.17.04-x64")
            for UNSUPPORTED_RID in "${UNSUPPORTED_RIDS[@]}"
            do
                if [[ ${__DistroRid} == $UNSUPPORTED_RID ]]; then
                    __isMSBuildOnNETCoreSupported=0
                    break
                fi
            done
        elif [ "$__HostOS" == "OSX" ]; then
            __isMSBuildOnNETCoreSupported=1
        elif [ "$__HostOS" == "FreeBSD" ]; then
            __isMSBuildOnNETCoreSupported=1
        fi
    fi
}


build_CoreLib_ni()
{
    local __CrossGenExec=$1

    if [ $__PartialNgen == 1 ]; then
        export COMPlus_PartialNGen=1
    fi

    if [ -e $__CrossGenCoreLibLog ]; then
        rm $__CrossGenCoreLibLog
    fi
    echo "Generating native image of System.Private.CoreLib.dll for $__BuildOS.$__BuildArch.$__BuildType. Logging to \"$__CrossGenCoreLibLog\"."
    echo "$__CrossGenExec /Platform_Assemblies_Paths $__BinDir/IL $__IbcTuning /out $__BinDir/System.Private.CoreLib.dll $__BinDir/IL/System.Private.CoreLib.dll"
    $__CrossGenExec /Platform_Assemblies_Paths $__BinDir/IL $__IbcTuning /out $__BinDir/System.Private.CoreLib.dll $__BinDir/IL/System.Private.CoreLib.dll >> $__CrossGenCoreLibLog 2>&1
    if [ $? -ne 0 ]; then
        echo "Failed to generate native image for System.Private.CoreLib. Refer to $__CrossGenCoreLibLog"
        exit 1
    fi

    if [ "$__BuildOS" == "Linux" ]; then
        echo "Generating symbol file for System.Private.CoreLib.dll"
        echo "$__CrossGenExec /Platform_Assemblies_Paths $__BinDir /CreatePerfMap $__BinDir $__BinDir/System.Private.CoreLib.dll"
        $__CrossGenExec /Platform_Assemblies_Paths $__BinDir /CreatePerfMap $__BinDir $__BinDir/System.Private.CoreLib.dll >> $__CrossGenCoreLibLog 2>&1
        if [ $? -ne 0 ]; then
            echo "Failed to generate symbol file for System.Private.CoreLib. Refer to $__CrossGenCoreLibLog"
            exit 1
        fi
    fi
}

build_CoreLib()
{
    if [ $__isMSBuildOnNETCoreSupported == 0 ]; then
        echo "System.Private.CoreLib.dll build unsupported."
        return
    fi

    if [ $__SkipMSCorLib == 1 ]; then
       echo "Skipping building System.Private.CoreLib."
       return
    fi

    echo "Commencing build of managed components for $__BuildOS.$__BuildArch.$__BuildType"

    # Invoke MSBuild
    __ExtraBuildArgs=""
    if [[ "$__IbcTuning" == "" ]]; then
        __ExtraBuildArgs="$__ExtraBuildArgs /p:OptimizationDataDir=\"$__PackagesDir/optimization.$__BuildOS-$__BuildArch.IBC.CoreCLR/$__IbcOptDataVersion/data\""
        __ExtraBuildArgs="$__ExtraBuildArgs /p:EnableProfileGuidedOptimization=true"
    fi

    if [[ "$__BuildManagedTools" -eq "1" ]]; then
        __ExtraBuildArgs="$__ExtraBuildArgs /p:BuildManagedTools=true"
    fi

    $__ProjectRoot/dotnet.sh restore /nologo /verbosity:minimal /clp:Summary \
                             /l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll\;LogFile=binclash.log \
                             /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true \
                             /p:UsePartialNGENOptimization=false /maxcpucount /p:IncludeRestoreOnlyProjects=true /p:ArcadeBuild=true\
                             $__ProjectDir/src/build.proj \
                             /flp:Verbosity=normal\;LogFile=$__LogsDir/System.Private.CoreLib_$__BuildOS__$__BuildArch__$__BuildType.log \
                             /p:__IntermediatesDir=$__IntermediatesDir /p:__RootBinDir=$__RootBinDir /p:BuildNugetPackage=false \
                             $__CommonMSBuildArgs $__ExtraBuildArgs $__UnprocessedBuildArgs

    if [ $? -ne 0 ]; then
        echo "Failed to restore managed components."
        exit 1
    fi

    $__ProjectRoot/dotnet.sh msbuild /nologo /verbosity:minimal /clp:Summary \
                             /l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll\;LogFile=binclash.log \
                             /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true \
                             /p:UsePartialNGENOptimization=false /maxcpucount /p:DotNetUseShippingVersions=true /p:ArcadeBuild=true\
                             $__ProjectDir/src/build.proj \
                             /flp:Verbosity=normal\;LogFile=$__LogsDir/System.Private.CoreLib_$__BuildOS__$__BuildArch__$__BuildType.log \
                             /p:__IntermediatesDir=$__IntermediatesDir /p:__RootBinDir=$__RootBinDir /p:BuildNugetPackage=false \
                             $__CommonMSBuildArgs $__ExtraBuildArgs $__UnprocessedBuildArgs

    if [ $? -ne 0 ]; then
        echo "Failed to build managed components."
        exit 1
    fi

    if [ $__SkipCrossgen == 1 ]; then
        echo "Skipping generating native image"
        return
    fi

    # The cross build generates a crossgen with the target architecture.
    if [ $__CrossBuild == 0 ]; then
       if [ $__SkipCoreCLR == 1 ]; then
           return
       fi

       # The architecture of host pc must be same architecture with target.
       if [[ ( "$__HostArch" == "$__BuildArch" ) ]]; then
           build_CoreLib_ni "$__BinDir/crossgen"
       elif [[ ( "$__HostArch" == "x64" ) && ( "$__BuildArch" == "x86" ) ]]; then
           build_CoreLib_ni "$__BinDir/crossgen"
       elif [[ ( "$__HostArch" == "arm64" ) && ( "$__BuildArch" == "arm" ) ]]; then
           build_CoreLib_ni "$__BinDir/crossgen"
       else
           exit 1
       fi
    else
       if [[ ( "$__CrossArch" == "x86" ) && ( "$__BuildArch" == "arm" ) ]]; then
           build_CoreLib_ni "$__CrossComponentBinDir/crossgen"
       elif [[ ( "$__CrossArch" == "x64" ) && ( "$__BuildArch" == "arm" ) ]]; then
           build_CoreLib_ni "$__CrossComponentBinDir/crossgen"
       elif [[ ( "$__HostArch" == "x64" ) && ( "$__BuildArch" == "arm64" ) ]]; then
           build_CoreLib_ni "$__CrossComponentBinDir/crossgen"
       fi
    fi
}

generate_NugetPackages()
{
    # We can only generate nuget package if we also support building mscorlib as part of this build.
    if [ $__isMSBuildOnNETCoreSupported == 0 ]; then
        echo "Nuget package generation unsupported."
        return
    fi

    # Since we can build mscorlib for this OS, did we build the native components as well?
    if [[ $__SkipCoreCLR == 1 && $__CrossgenOnly == 0 ]]; then
        echo "Unable to generate nuget packages since native components were not built."
        return
    fi

    echo "Generating nuget packages for "$__BuildOS
    echo "DistroRid is "$__DistroRid
    echo "ROOTFS_DIR is "$ROOTFS_DIR
    # Build the packages
    $__ProjectRoot/dotnet.sh msbuild /nologo /verbosity:minimal /clp:Summary \
                             /l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll\;LogFile=binclash.log \
                             /p:RestoreDefaultOptimizationDataPackage=false /p:PortableBuild=true \
                             /p:UsePartialNGENOptimization=false /maxcpucount \
                             $__SourceDir/.nuget/packages.builds \
                             /flp:Verbosity=normal\;LogFile=$__LogsDir/Nuget_$__BuildOS__$__BuildArch__$__BuildType.log \
                             /p:__IntermediatesDir=$__IntermediatesDir /p:__RootBinDir=$__RootBinDir /p:BuildNugetPackages=false /p:__DoCrossArchBuild=$__CrossBuild \
                             $__CommonMSBuildArgs $__UnprocessedBuildArgs

    if [ $? -ne 0 ]; then
        echo "Failed to generate Nuget packages."
        exit 1
    fi
}

echo "Commencing CoreCLR Repo build"

# Argument types supported by this script:
#
# Build architecture - valid values are: x64, ARM.
# Build Type         - valid values are: Debug, Checked, Release
#
# Set the default arguments for build

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Use uname to determine what the CPU is.
CPUName=$(uname -p)
# Some Linux platforms report unknown for platform, but the arch for machine.
if [ "$CPUName" == "unknown" ]; then
    CPUName=$(uname -m)
fi

case $CPUName in
    i686)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=x86
        __HostArch=x86
        ;;

    x86_64)
        __BuildArch=x64
        __HostArch=x64
        ;;

    armv7l)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=arm
        __HostArch=arm
        ;;

    aarch64)
        __BuildArch=arm64
        __HostArch=arm64
        ;;

    amd64)
        __BuildArch=x64
        __HostArch=x64
        ;;
    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        __BuildArch=x64
        __HostArch=x64
        ;;
esac

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        __HostOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        __HostOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        __HostOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        __HostOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        __HostOS=NetBSD
        ;;

    SunOS)
        __BuildOS=SunOS
        __HostOS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        __HostOS=Linux
        ;;
esac

__BuildType=Debug
__CodeCoverage=
__IgnoreWarnings=0

# Set the various build properties here so that CMake and MSBuild can pick them up
__ProjectDir="$__ProjectRoot"
__SourceDir="$__ProjectDir/src"
__PackagesDir="${DotNetRestorePackagesPath:-${__ProjectDir}/packages}"
__RootBinDir="$__ProjectDir/bin"
__UnprocessedBuildArgs=
__CommonMSBuildArgs=
__MSBCleanBuildArgs=
__UseNinja=0
__VerboseBuild=0
__PgoInstrument=0
__PgoOptimize=1
__IbcTuning=""
__ConfigureOnly=0
__SkipConfigure=0
__SkipManaged=0
__SkipRestore=""
__SkipNuget=0
__SkipCoreCLR=0
__SkipCrossArchNative=0
__SkipMSCorLib=0
__SkipRestoreOptData=0
__SkipCrossgen=0
__CrossgenOnly=0
__PartialNgen=0
__SkipTests=0
__CrossBuild=0
__ClangMajorVersion=0
__ClangMinorVersion=0
__GccBuild=0
__GccMajorVersion=0
__GccMinorVersion=0
__NuGetPath="$__PackagesDir/NuGet.exe"
__DistroRid=""
__cmakeargs=""
__SkipGenerateVersion=0
__PortableBuild=1
__msbuildonunsupportedplatform=0
__PgoOptDataVersion=""
__IbcOptDataVersion=""
__BuildManagedTools=1
__SkipRestoreArg="/p:RestoreDuringBuild=true"
__SignTypeArg=""
__OfficialBuildIdArg=""
__StaticAnalyzer=0

# Get the number of processors available to the scheduler
# Other techniques such as `nproc` only get the number of
# processors available to a single process.
if [ `uname` = "FreeBSD" ]; then
  __NumProc=`sysctl hw.ncpu | awk '{ print $2+1 }'`
elif [ `uname` = "NetBSD" ]; then
  __NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
elif [ `uname` = "Darwin" ]; then
  __NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
else
  __NumProc=$(nproc --all)
fi

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -\?|-h|--help)
            usage
            exit 1
            ;;

        x86|-x86)
            __BuildArch=x86
            ;;

        x64|-x64)
            __BuildArch=x64
            ;;

        arm|-arm)
            __BuildArch=arm
            ;;

        armel|-armel)
            __BuildArch=armel
            ;;

        arm64|-arm64)
            __BuildArch=arm64
            ;;

        debug|-debug)
            __BuildType=Debug
            ;;

        checked|-checked)
            __BuildType=Checked
            ;;

        release|-release)
            __BuildType=Release
            ;;

        coverage|-coverage)
            __CodeCoverage=Coverage
            ;;

        cross|-cross)
            __CrossBuild=1
            ;;

        -portablebuild=false)
            __PortableBuild=0
            ;;

        verbose|-verbose)
            __VerboseBuild=1
            ;;

        stripsymbols|-stripsymbols)
            __cmakeargs="$__cmakeargs -DSTRIP_SYMBOLS=true"
            ;;

        clang3.5|-clang3.5)
            __ClangMajorVersion=3
            __ClangMinorVersion=5
            ;;

        clang3.6|-clang3.6)
            __ClangMajorVersion=3
            __ClangMinorVersion=6
            ;;

        clang3.7|-clang3.7)
            __ClangMajorVersion=3
            __ClangMinorVersion=7
            ;;

        clang3.8|-clang3.8)
            __ClangMajorVersion=3
            __ClangMinorVersion=8
            ;;

        clang3.9|-clang3.9)
            __ClangMajorVersion=3
            __ClangMinorVersion=9
            ;;

        clang4.0|-clang4.0)
            __ClangMajorVersion=4
            __ClangMinorVersion=0
            ;;

        clang5.0|-clang5.0)
            __ClangMajorVersion=5
            __ClangMinorVersion=0
            ;;

        clang6.0|-clang6.0)
            __ClangMajorVersion=6
            __ClangMinorVersion=0
            ;;

        clang7|-clang7)
            __ClangMajorVersion=7
            __ClangMinorVersion=
            ;;

        gcc5|-gcc5)
            __GccMajorVersion=5
            __GccMinorVersion=
            __GccBuild=1
            ;;

        gcc6|-gcc6)
            __GccMajorVersion=6
            __GccMinorVersion=
            __GccBuild=1
            ;;

        gcc7|-gcc7)
            __GccMajorVersion=7
            __GccMinorVersion=
            __GccBuild=1
            ;;

        gcc8|-gcc8)
            __GccMajorVersion=8
            __GccMinorVersion=
            __GccBuild=1
            ;;

        gcc|-gcc)
            __GccMajorVersion=
            __GccMinorVersion=
            __GccBuild=1
            ;;

        ninja|-ninja)
            __UseNinja=1
            ;;

        pgoinstrument|-pgoinstrument)
            __PgoInstrument=1
            ;;

        nopgooptimize|-nopgooptimize)
            __PgoOptimize=0
            __SkipRestoreOptData=1
            ;;

        ibcinstrument|-ibcinstrument)
            __IbcTuning="/Tuning"
            ;;

        configureonly|-configureonly)
            __ConfigureOnly=1
            __SkipMSCorLib=1
            __SkipNuget=1
            ;;

        skipconfigure|-skipconfigure)
            __SkipConfigure=1
            ;;

        skipnative|-skipnative)
            # Use "skipnative" to use the same option name as build.cmd.
            __SkipCoreCLR=1
            ;;

        skipcoreclr|-skipcoreclr)
            # Accept "skipcoreclr" for backwards-compatibility.
            __SkipCoreCLR=1
            ;;

        skipcrossarchnative|-skipcrossarchnative)
            __SkipCrossArchNative=1
            ;;

        skipmanaged|-skipmanaged)
            __SkipManaged=1
            ;;

        skipmscorlib|-skipmscorlib)
            __SkipMSCorLib=1
            ;;

        skipgenerateversion|-skipgenerateversion)
            __SkipGenerateVersion=1
            ;;

        skiprestoreoptdata|-skiprestoreoptdata)
            __SkipRestoreOptData=1
            ;;

        skipcrossgen|-skipcrossgen)
            __SkipCrossgen=1
            ;;

        crossgenonly|-crossgenonly)
            __SkipMSCorLib=1
            __SkipCoreCLR=1
            __CrossgenOnly=1
            ;;
        partialngen|-partialngen)
            __PartialNgen=1
            ;;

        skiptests|-skiptests)
            __SkipTests=1
            ;;

        skipnuget|-skipnuget|skipbuildpackages|-skipbuildpackages)
            __SkipNuget=1
            ;;

        ignorewarnings|-ignorewarnings)
            __IgnoreWarnings=1
            __cmakeargs="$__cmakeargs -DCLR_CMAKE_WARNINGS_ARE_ERRORS=OFF"
            ;;

        cmakeargs|-cmakeargs)
            if [ -n "$2" ]; then
                __cmakeargs="$__cmakeargs $2"
                shift
            else
                echo "ERROR: 'cmakeargs' requires a non-empty option argument"
                exit 1
            fi
            ;;

        bindir|-bindir)
            if [ -n "$2" ]; then
                __RootBinDir="$2"
                if [ ! -d $__RootBinDir ]; then
                    mkdir $__RootBinDir
                fi
                __RootBinParent=$(dirname $__RootBinDir)
                __RootBinName=${__RootBinDir##*/}
                __RootBinDir="$(cd $__RootBinParent &>/dev/null && printf %s/%s $PWD $__RootBinName)"
                shift
            else
                echo "ERROR: 'bindir' requires a non-empty option argument"
                exit 1
            fi
            ;;
        msbuildonunsupportedplatform|-msbuildonunsupportedplatform)
            __msbuildonunsupportedplatform=1
            ;;
        numproc|-numproc)
            if [ -n "$2" ]; then
              __NumProc="$2"
              shift
            else
              echo "ERROR: 'numproc' requires a non-empty option argument"
              exit 1
            fi
            ;;
        osgroup|-osgroup)
            if [ -n "$2" ]; then
              __BuildOS="$2"
              shift
            else
              echo "ERROR: 'osgroup' requires a non-empty option argument"
              exit 1
            fi
            ;;
        rebuild|-rebuild)
            echo "ERROR: 'Rebuild' is not supported.  Please remove it."
            exit 1
            ;;

        -skiprestore)
            __SkipRestoreArg="/p:RestoreDuringBuild=false"
            ;;

        -disableoss)
            __SignTypeArg="/p:SignType=real"
            ;;

        -officialbuildid=*)
            __Id=$(echo $1| cut -d'=' -f 2)
            __OfficialBuildIdArg="/p:OfficialBuildId=$__Id"
            ;;

        -staticanalyzer)
            __StaticAnalyzer=1
            ;;

        --)
            # Skip -Option=Value style argument passing
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac

    shift
done

__CommonMSBuildArgs="/p:__BuildArch=$__BuildArch /p:__BuildType=$__BuildType /p:__BuildOS=$__BuildOS $__OfficialBuildIdArg $__SignTypeArg $__SkipRestoreArg"

# Configure environment if we are doing a verbose build
if [ $__VerboseBuild == 1 ]; then
    export VERBOSE=1
    __CommonMSBuildArgs="$__CommonMSBuildArgs /v:detailed"
fi

# Set default clang version
if [[ $__ClangMajorVersion == 0 && $__ClangMinorVersion == 0 ]]; then
    if [[ "$__BuildArch" == "arm" || "$__BuildArch" == "armel" ]]; then
        __ClangMajorVersion=5
        __ClangMinorVersion=0
    else
        __ClangMajorVersion=3
        __ClangMinorVersion=9
    fi
fi

if [[ "$__BuildArch" == "armel" ]]; then
    # Armel cross build is Tizen specific and does not support Portable RID build
    __PortableBuild=0
fi

if [ $__PortableBuild == 0 ]; then
    __CommonMSBuildArgs="$__CommonMSBuildArgs /p:PortableBuild=false"
fi

# Set dependent variables
__LogsDir="$__RootBinDir/Logs"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/Product/$__BuildOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__ToolsDir="$__RootBinDir/tools"
__TestWorkingDir="$__RootBinDir/tests/$__BuildOS.$__BuildArch.$__BuildType"
export __IntermediatesDir="$__RootBinDir/obj/$__BuildOS.$__BuildArch.$__BuildType"
__TestIntermediatesDir="$__RootBinDir/tests/obj/$__BuildOS.$__BuildArch.$__BuildType"
__isMSBuildOnNETCoreSupported=0
__CrossComponentBinDir="$__BinDir"

__CrossArch="$__HostArch"
if [ $__CrossBuild == 1 ]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossGenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__BuildOS.$__BuildArch.$__BuildType.log"

# init the target distro name
initTargetDistroRid

# Init if MSBuild for .NET Core is supported for this platform
isMSBuildOnNETCoreSupported

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to set.
# This is needed by CLI to function.
if [ -z "$HOME" ]; then
    if [ ! -d "$__ProjectDir/temp_home" ]; then
        mkdir temp_home
    fi
    export HOME=$__ProjectDir/temp_home
    echo "HOME not defined; setting it to $HOME"
fi

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built CoreClr libraries will copied to.
export __CMakeBinDir="$__BinDir"

# Configure environment if we are doing a cross compile.
if [ $__CrossBuild == 1 ]; then
    export CROSSCOMPILE=1
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        export ROOTFS_DIR="$__ProjectRoot/cross/rootfs/$__BuildArch"
    fi
fi

# Make the directories necessary for build if they don't exist
setup_dirs

# Set up the directory for MSBuild debug logs.
export MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"

# Check prereqs.
check_prereqs

# Restore the package containing profile counts for profile-guided optimizations
restore_optdata

# Generate event logging infrastructure sources
generate_event_logging

# Build the coreclr (native) components.
__ExtraCmakeArgs="-DCLR_CMAKE_TARGET_OS=$__BuildOS -DCLR_CMAKE_PACKAGES_DIR=$__PackagesDir -DCLR_CMAKE_PGO_INSTRUMENT=$__PgoInstrument -DCLR_CMAKE_OPTDATA_VERSION=$__PgoOptDataVersion -DCLR_CMAKE_PGO_OPTIMIZE=$__PgoOptimize"

# [TODO] Remove this when the `build-test.sh` script properly builds and deploys test assets.
if [ $__SkipTests != 1 ]; then
    echo "Adding CMake flags to build native tests for $__BuildOS.$__BuildArch.$__BuildType"
    __ExtraCmakeArgs="$__ExtraCmakeArgs -DCLR_CMAKE_BUILD_TESTS=ON"
fi

build_native $__SkipCoreCLR "$__BuildArch" "$__IntermediatesDir" "$__ExtraCmakeArgs" "CoreCLR component"

# Build cross-architecture components
if [ $__SkipCrossArchNative != 1 ]; then
    if [[ $__CrossBuild == 1 ]]; then
        build_cross_architecture_components
    fi
fi

# Build System.Private.CoreLib.

build_CoreLib

if [ $__CrossgenOnly == 1 ]; then
    build_CoreLib_ni "$__BinDir/crossgen"
fi

# Generate nuget packages
if [ $__SkipNuget != 1 ]; then
    generate_NugetPackages
fi


# Build complete

echo "Repo successfully built."
echo "Product binaries are available at $__BinDir"
exit 0
