#!/usr/bin/env bash

PYTHON=${PYTHON:-python}

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [clean] [verbose] [coverage] [cross] [clangx.y] [ninja] [configureonly] [skipconfigure] [skipnative] [skipmscorlib] [skiptests] [cmakeargs] [bindir]"
    echo "BuildArch can be: x64, x86, arm, arm-softfp, arm64"
    echo "BuildType can be: debug, checked, release"
    echo "clean - optional argument to force a clean build."
    echo "verbose - optional argument to enable verbose build output."
    echo "coverage - optional argument to enable code coverage build (currently supported only for Linux and OSX)."
    echo "ninja - target ninja instead of GNU make"
    echo "clangx.y - optional argument to build using clang version x.y."
    echo "cross - optional argument to signify cross compilation,"
    echo "      - will use ROOTFS_DIR environment variable if set."
    echo "configureonly - do not perform any builds; just configure the build."
    echo "skipconfigure - skip build configuration."
    echo "skipnative - do not build native components."
    echo "skipmscorlib - do not build mscorlib.dll."
    echo "skiptests - skip the tests in the 'tests' subdirectory."
    echo "skiprestore - skip restoring nuget packages."
    echo "skipnuget - skip building nuget packages."
    echo "disableoss - Disable Open Source Signing for mscorlib."
    echo "skipgenerateversion - disable version generation even if MSBuild is supported."
    echo "cmakeargs - user-settable additional arguments passed to CMake."
    echo "bindir - output directory (defaults to $__ProjectRoot/bin)"

    exit 1
}

initDistroRid()
{
    if [ "$__BuildOS" == "Linux" ]; then
        if [ ! -e /etc/os-release ]; then
            echo "WARNING: Can not determine runtime id for current distro."
            export __DistroRid=""
        else
            source /etc/os-release
            export __DistroRid="$ID.$VERSION_ID-$__HostArch"
        fi
    fi
}

setup_dirs()
{
    echo Setting up directories for build

    mkdir -p "$__RootBinDir"
    mkdir -p "$__BinDir"
    mkdir -p "$__LogsDir"
    mkdir -p "$__IntermediatesDir"
}

# Performs "clean build" type actions (deleting and remaking directories)

clean()
{
    echo Cleaning previous output for the selected configuration
    rm -rf "$__BinDir"
    rm -rf "$__IntermediatesDir"

    rm -rf "$__TestWorkingDir"
    rm -rf "$__TestIntermediatesDir"

    rm -rf "$__LogsDir/*_$__BuildOS__$__BuildArch__$__BuildType.*"
    rm -rf "$__ProjectRoot/Tools"
}

# Check the system to ensure the right prereqs are in place

check_prereqs()
{
    echo "Checking prerequisites..."

    # Check presence of CMake on the path
    hash cmake 2>/dev/null || { echo >&2 "Please install cmake before running this script"; exit 1; }

    # Check for clang
    hash clang-$__ClangMajorVersion.$__ClangMinorVersion 2>/dev/null ||  hash clang$__ClangMajorVersion$__ClangMinorVersion 2>/dev/null ||  hash clang 2>/dev/null || { echo >&2 "Please install clang-$__ClangMajorVersion.$__ClangMinorVersion before running this script"; exit 1; }

}

build_coreclr()
{

    if [ $__SkipCoreCLR == 1 ]; then
        echo "Skipping CoreCLR build."
        return
    fi

# Event Logging Infrastructure
   __GeneratedIntermediate="$__IntermediatesDir/Generated"
   __GeneratedIntermediateEventProvider="$__GeneratedIntermediate/eventprovider_new"
    if [[ -d "$__GeneratedIntermediateEventProvider" ]]; then
        rm -rf  "$__GeneratedIntermediateEventProvider"
    fi

    if [[ ! -d "$__GeneratedIntermediate/eventprovider" ]]; then
        mkdir -p "$__GeneratedIntermediate/eventprovider"
    fi

    mkdir -p "$__GeneratedIntermediateEventProvider"
    if [[ $__SkipCoreCLR == 0 || $__ConfigureOnly == 1 ]]; then
        echo "Laying out dynamically generated files consumed by the build system "
        echo "Laying out dynamically generated Event Logging Test files"
        $PYTHON -B -Wall -Werror "$__ProjectRoot/src/scripts/genXplatEventing.py" --man "$__ProjectRoot/src/vm/ClrEtwAll.man" --exc "$__ProjectRoot/src/vm/ClrEtwAllMeta.lst" --testdir "$__GeneratedIntermediateEventProvider/tests"

        if  [[ $? != 0 ]]; then
            exit
        fi

        #determine the logging system
        case $__BuildOS in
            Linux)
                echo "Laying out dynamically generated Event Logging Implementation of Lttng"
                $PYTHON -B -Wall -Werror "$__ProjectRoot/src/scripts/genXplatLttng.py" --man "$__ProjectRoot/src/vm/ClrEtwAll.man" --intermediate "$__GeneratedIntermediateEventProvider"
                if  [[ $? != 0 ]]; then
                    exit
                fi
                ;;
            *)
                ;;
        esac
    fi

    echo "Cleaning the temp folder of dynamically generated Event Logging files"
    $PYTHON -B -Wall -Werror -c "import sys;sys.path.insert(0,\"$__ProjectRoot/src/scripts\"); from Utilities import *;UpdateDirectory(\"$__GeneratedIntermediate/eventprovider\",\"$__GeneratedIntermediateEventProvider\")"
    if  [[ $? != 0 ]]; then
        exit
    fi

    rm -rf "$__GeneratedIntermediateEventProvider"

    # All set to commence the build

    echo "Commencing build of native components for $__BuildOS.$__BuildArch.$__BuildType in $__IntermediatesDir"

    cd "$__IntermediatesDir"

    generator=""
    buildFile="Makefile"
    buildTool="make"
    if [ $__UseNinja == 1 ]; then
        generator="ninja"
        buildFile="build.ninja"
        buildTool="ninja"
    fi

    if [ $__SkipConfigure == 0 ]; then
        # if msbuild is not supported, then set __SkipGenerateVersion to 1
        if [ $__isMSBuildOnNETCoreSupported == 0 ]; then __SkipGenerateVersion=1; fi
        # Drop version.c file
        __versionSourceFile=$__IntermediatesDir/version.cpp
        if [ $__SkipGenerateVersion == 0 ]; then
            "$__ProjectRoot/init-tools.sh" > "$__ProjectRoot/init-tools.log"
            echo "Running: \"$__ProjectRoot/Tools/corerun\" \"$__ProjectRoot/Tools/MSBuild.exe\" \"$__ProjectRoot/build.proj\" /v:minimal /t:GenerateVersionSourceFile /p:NativeVersionSourceFile=$__versionSourceFile /p:GenerateVersionSourceFile=true /v:minimal $__OfficialBuildIdArg"
            "$__ProjectRoot/Tools/corerun" "$__ProjectRoot/Tools/MSBuild.exe" "$__ProjectRoot/build.proj" /v:minimal /t:GenerateVersionSourceFile /p:NativeVersionSourceFile=$__versionSourceFile /p:GenerateVersionSourceFile=true /v:minimal $__OfficialBuildIdArg
        else
            __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
            echo $__versionSourceLine > $__versionSourceFile
        fi
        # Regenerate the CMake solution
        echo "Invoking \"$__ProjectRoot/src/pal/tools/gen-buildsys-clang.sh\" \"$__ProjectRoot\" $__ClangMajorVersion $__ClangMinorVersion $__BuildArch $__BuildType $__CodeCoverage $__IncludeTests $generator $__cmakeargs"
        "$__ProjectRoot/src/pal/tools/gen-buildsys-clang.sh" "$__ProjectRoot" $__ClangMajorVersion $__ClangMinorVersion $__BuildArch $__BuildType $__CodeCoverage $__IncludeTests $generator "$__cmakeargs"
    fi

    # Check that the makefiles were created.

    if [ ! -f "$__IntermediatesDir/$buildFile" ]; then
        echo "Failed to generate native component build project!"
        exit 1
    fi

    # Get the number of processors available to the scheduler
    # Other techniques such as `nproc` only get the number of
    # processors available to a single process.
    if [ `uname` = "FreeBSD" ]; then
        NumProc=`sysctl hw.ncpu | awk '{ print $2+1 }'`
    elif [ `uname` = "NetBSD" ]; then
        NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
    else
        NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
    fi

    # Build CoreCLR

    echo "Executing $buildTool install -j $NumProc $__UnprocessedBuildArgs"

    $buildTool install -j $NumProc $__UnprocessedBuildArgs
    if [ $? != 0 ]; then
        echo "Failed to build coreclr components."
        exit 1
    fi
}

restoreBuildTools()
{
    echo "Restoring BuildTools..."
    $__ProjectRoot/init-tools.sh
    if [ $? -ne 0 ]; then
        echo "Failed to restore BuildTools."
        exit 1
    fi
}

isMSBuildOnNETCoreSupported()
{
    # This needs to be updated alongwith corresponding changes to netci.groovy.
    __isMSBuildOnNETCoreSupported=0

    if [ "$__HostArch" == "x64" ]; then
        if [ "$__HostOS" == "Linux" ]; then
            case "$__DistroRid" in
                "centos.7-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "debian.8-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "fedora.23-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "opensuse.13.2-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "rhel.7.2-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "ubuntu.14.04-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "ubuntu.16.04-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                *)
            esac
        elif [ "$__HostOS" == "OSX" ]; then
            __isMSBuildOnNETCoreSupported=1
        fi
    fi
}

build_CoreLib_ni()
{
    if [ $__SkipCoreCLR == 0 -a -e $__BinDir/crossgen ]; then
        echo "Generating native image for System.Private.CoreLib."
        $__BinDir/crossgen $__BinDir/System.Private.CoreLib.dll
        if [ $? -ne 0 ]; then
            echo "Failed to generate native image for System.Private.CoreLib."
            exit 1
        fi

        echo "Generating native image for MScorlib Facade."
        $__BinDir/crossgen $__BinDir/mscorlib.dll
        if [ $? -ne 0 ]; then
            echo "Failed to generate native image for mscorlib facade."
            exit 1
        fi

        if [ "$__BuildOS" == "Linux" ]; then
            echo "Generating symbol file for System.Private.CoreLib."
            $__BinDir/crossgen /CreatePerfMap $__BinDir $__BinDir/System.Private.CoreLib.ni.dll
            if [ $? -ne 0 ]; then
                echo "Failed to generate symbol file for System.Private.CoreLib."
                exit 1
            fi
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

    # Restore buildTools

    restoreBuildTools

    echo "Commencing build of managed components for $__BuildOS.$__BuildArch.$__BuildType"

    # Invoke MSBuild
    $__ProjectRoot/Tools/corerun "$__MSBuildPath" /nologo "$__ProjectRoot/build.proj" /verbosity:minimal "/fileloggerparameters:Verbosity=normal;LogFile=$__LogsDir/System.Private.CoreLib_$__BuildOS__$__BuildArch__$__BuildType.log" /t:Build /p:__BuildOS=$__BuildOS /p:__BuildArch=$__BuildArch /p:__BuildType=$__BuildType /p:__IntermediatesDir=$__IntermediatesDir /p:__RootBinDir=$__RootBinDir /p:BuildNugetPackage=false /p:UseSharedCompilation=false ${__SignTypeReal}

    if [ $? -ne 0 ]; then
        echo "Failed to build managed components."
        exit 1
    fi

    # The cross build generates a crossgen with the target architecture.
    if [ $__CrossBuild != 1 ]; then
       # The architecture of host pc must be same architecture with target.
       if [[ ( "$__HostArch" == "$__BuildArch" ) ]]; then
           build_CoreLib_ni
       elif [[ ( "$__HostArch" == "x64" ) && ( "$__BuildArch" == "x86" ) ]]; then
           build_CoreLib_ni
       elif [[ ( "$__HostArch" == "arm64" ) && ( "$__BuildArch" == "arm" ) ]]; then
           build_CoreLib_ni
       else 
           exit 1
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
    if [ $__SkipCoreCLR == 1 ]; then
        echo "Unable to generate nuget packages since native components were not built."
        return
    fi

    if [ $__SkipMSCorLib == 1 ]; then
        # Restore buildTools, since we skipped doing so with the mscorlib build.
        restoreBuildTools

       echo "Unable to generate Microsoft.NETCore.Runtime.CoreCLR nuget package since mscorlib was not built."
    fi

    echo "Generating nuget packages for "$__BuildOS

    # Build the packages
    $__ProjectRoot/Tools/corerun "$__MSBuildPath" /nologo "$__ProjectRoot/src/.nuget/packages.builds" /verbosity:minimal "/fileloggerparameters:Verbosity=normal;LogFile=$__LogsDir/Nuget_$__BuildOS__$__BuildArch__$__BuildType.log" /t:Build /p:__BuildOS=$__BuildOS /p:__BuildArch=$__BuildArch /p:__BuildType=$__BuildType /p:__IntermediatesDir=$__IntermediatesDir /p:__RootBinDir=$__RootBinDir /p:BuildNugetPackage=false /p:UseSharedCompilation=false

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
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=arm64
        __HostArch=arm64
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
__IncludeTests=Include_Tests

# Set the various build properties here so that CMake and MSBuild can pick them up
__ProjectDir="$__ProjectRoot"
__SourceDir="$__ProjectDir/src"
__PackagesDir="$__ProjectDir/packages"
__RootBinDir="$__ProjectDir/bin"
__UnprocessedBuildArgs=
__MSBCleanBuildArgs=
__UseNinja=0
__ConfigureOnly=0
__SkipConfigure=0
__SkipRestore=""
__SkipNuget=0
__SkipCoreCLR=0
__SkipMSCorLib=0
__CleanBuild=0
__VerboseBuild=0
__SignTypeReal=""
__CrossBuild=0
__ClangMajorVersion=0
__ClangMinorVersion=0
__MSBuildPath=$__ProjectRoot/Tools/MSBuild.exe
__NuGetPath="$__PackagesDir/NuGet.exe"
__DistroRid=""
__cmakeargs=""
__OfficialBuildIdArg=
__SkipGenerateVersion=0

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

        x86)
            __BuildArch=x86
            ;;

        x64)
            __BuildArch=x64
            ;;

        arm)
            __BuildArch=arm
            ;;

        arm-softfp)
            __BuildArch=arm-softfp
            ;;

        arm64)
            __BuildArch=arm64
            ;;

        debug)
            __BuildType=Debug
            ;;

        checked)
            __BuildType=Checked
            ;;

        release)
            __BuildType=Release
            ;;

        coverage)
            __CodeCoverage=Coverage
            ;;

        clean)
            __CleanBuild=1
            ;;

        verbose)
            __VerboseBuild=1
            ;;

        cross)
            __CrossBuild=1
            ;;

        clang3.5)
            __ClangMajorVersion=3
            __ClangMinorVersion=5
            ;;

        clang3.6)
            __ClangMajorVersion=3
            __ClangMinorVersion=6
            ;;

        clang3.7)
            __ClangMajorVersion=3
            __ClangMinorVersion=7
            ;;

        clang3.8)
            __ClangMajorVersion=3
            __ClangMinorVersion=8
            ;;

        ninja)
            __UseNinja=1
            ;;

        configureonly)
            __ConfigureOnly=1
            __SkipCoreCLR=1
            __SkipMSCorLib=1
            __IncludeTests=
            ;;

        skipconfigure)
            __SkipConfigure=1
            ;;

        skipnative)
            # Use "skipnative" to use the same option name as build.cmd.
            __SkipCoreCLR=1
            ;;

        skipcoreclr)
            # Accept "skipcoreclr" for backwards-compatibility.
            __SkipCoreCLR=1
            ;;

        skipmscorlib)
            __SkipMSCorLib=1
            ;;

        skipgenerateversion)
            __SkipGenerateVersion=1
            ;;

        includetests)
            ;;

        skiptests)
            __IncludeTests=
            ;;

        skiprestore)
            __SkipRestore="/p:RestoreDuringBuild=true"
            ;;

        skipnuget)
            __SkipNuget=1
            ;;

        disableoss)
            __SignTypeReal="/p:SignType=real"
            ;;

        cmakeargs)
            if [ -n "$2" ]; then
                __cmakeargs="$2"
                shift
            else
                echo "ERROR: 'cmakeargs' requires a non-empty option argument"
                exit 1
            fi
            ;;

        bindir)
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

        *)
            if [[ $1 == "/p:OfficialBuildId="* ]]; then
                __OfficialBuildIdArg=$1
            else
                __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            fi
            ;;
    esac

    shift
done

# Set default clang version
if [[ $__ClangMajorVersion == 0 && $__ClangMinorVersion == 0 ]]; then
    if [ $__CrossBuild == 1 ]; then
        __ClangMajorVersion=3
        __ClangMinorVersion=6
    else
        __ClangMajorVersion=3
        __ClangMinorVersion=5
    fi
fi

# Set dependent variables
__LogsDir="$__RootBinDir/Logs"

if [[ $__ConfigureOnly == 1 && $__SkipConfigure == 1 ]]; then
    echo "configureonly and skipconfigure are mutually exclusive!"
    exit 1
fi

# init the distro name
initDistroRid

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/Product/$__BuildOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__ToolsDir="$__RootBinDir/tools"
__TestWorkingDir="$__RootBinDir/tests/$__BuildOS.$__BuildArch.$__BuildType"
export __IntermediatesDir="$__RootBinDir/obj/$__BuildOS.$__BuildArch.$__BuildType"
__TestIntermediatesDir="$__RootBinDir/tests/obj/$__BuildOS.$__BuildArch.$__BuildType"
__isMSBuildOnNETCoreSupported=0

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

# Configure environment if we are doing a clean build.
if [ $__CleanBuild == 1 ]; then
    clean
fi

# Configure environment if we are doing a verbose build
if [ $__VerboseBuild == 1 ]; then
    export VERBOSE=1
fi

# Configure environment if we are doing a cross compile.
if [ $__CrossBuild == 1 ]; then
    export CROSSCOMPILE=1
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        export ROOTFS_DIR="$__ProjectRoot/cross/rootfs/$__BuildArch"
    fi
fi

# Make the directories necessary for build if they don't exist

setup_dirs

# Check prereqs.

check_prereqs

# Build the coreclr (native) components.

build_coreclr

# Build System.Private.CoreLib.

build_CoreLib

# Generate nuget packages
if [ $__SkipNuget != 1 ]; then
    generate_NugetPackages
fi


# Build complete

echo "Repo successfully built."
echo "Product binaries are available at $__BinDir"
exit 0
