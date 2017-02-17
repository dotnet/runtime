#!/usr/bin/env bash

# resolve python-version to use
if [ "$PYTHON" == "" ] ; then
    if ! PYTHON=$(command -v python || command -v python2 || command -v python 2.7)
    then
       echo "Unable to locate build-dependency python2.x!" 1>&2
       exit 1
    fi
fi

# validate python-dependency
# useful in case of explicitly set option.
if ! command -v $PYTHON > /dev/null
then
   echo "Unable to locate build-dependency python2.x ($PYTHON)!" 1>&2
   exit 1
fi

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [verbose] [coverage] [cross] [clangx.y] [ninja] [configureonly] [skipconfigure] [skipnative] [skipmscorlib] [skiptests] [cmakeargs] [bindir]"
    echo "BuildArch can be: x64, x86, arm, armel, arm64"
    echo "BuildType can be: debug, checked, release"
    echo "coverage - optional argument to enable code coverage build (currently supported only for Linux and OSX)."
    echo "ninja - target ninja instead of GNU make"
    echo "clangx.y - optional argument to build using clang version x.y."
    echo "cross - optional argument to signify cross compilation,"
    echo "      - will use ROOTFS_DIR environment variable if set."
    echo "crosscomponent - optional argument to build cross-architecture component,"
    echo "               - will use CAC_ROOTFS_DIR environment variable if set."
    echo "pgoinstrument - generate instrumented code for profile guided optimization enabled binaries."
    echo "configureonly - do not perform any builds; just configure the build."
    echo "skipconfigure - skip build configuration."
    echo "skipnative - do not build native components."
    echo "skipmscorlib - do not build mscorlib.dll."
    echo "skiptests - skip the tests in the 'tests' subdirectory."
    echo "skipnuget - skip building nuget packages."
    echo "portableLinux - build for Portable Linux Distribution"
    echo "verbose - optional argument to enable verbose build output."
    echo "-skiprestore: skip restoring packages ^(default: packages are restored during build^)."
	echo "-disableoss: Disable Open Source Signing for System.Private.CoreLib."
	echo "-sequential: force a non-parallel build ^(default is to build in parallel"
	echo "   using all processors^)."
	echo "-officialbuildid=^<ID^>: specify the official build ID to be used by this build."
	echo "-Rebuild: passes /t:rebuild to the build projects."
    echo "skipgenerateversion - disable version generation even if MSBuild is supported."
    echo "cmakeargs - user-settable additional arguments passed to CMake."
    echo "bindir - output directory (defaults to $__ProjectRoot/bin)"
    echo "buildstandalonegc - builds the GC in a standalone mode. Can't be used with \"cmakeargs\"."
    echo "msbuildonunsupportedplatform - build managed binaries even if distro is not officially supported."
    exit 1
}

initHostDistroRid()
{
    if [ "$__HostOS" == "Linux" ]; then
        if [ ! -e /etc/os-release ]; then
            echo "WARNING: Can not determine runtime id for current distro."
            __HostDistroRid=""
        else
            source /etc/os-release
            __HostDistroRid="$ID.$VERSION_ID-$__HostArch"
        fi
    fi
}

initTargetDistroRid()
{
    if [ $__CrossBuild == 1 ]; then
        if [ "$__BuildOS" == "Linux" ]; then
            if [ ! -e $ROOTFS_DIR/etc/os-release ]; then
                echo "WARNING: Can not determine runtime id for current distro."
                export __DistroRid=""
            else
                source $ROOTFS_DIR/etc/os-release
                export __DistroRid="$ID.$VERSION_ID-$__BuildArch"
            fi
        fi
    else
        export __DistroRid="$__HostDistroRid"
    fi

    # Portable builds target the base RID only for Linux based platforms
    if [ $__PortableLinux == 1 ]; then
        export __DistroRid="linux-$__BuildArch"
    fi
}

setup_dirs()
{
    echo Setting up directories for build

    mkdir -p "$__RootBinDir"
    mkdir -p "$__BinDir"
    mkdir -p "$__LogsDir"
    mkdir -p "$__IntermediatesDir"

    if [ $__CrossBuild == 1 ]; then
        mkdir -p "$__CrossComponentBinDir"
        mkdir -p "$__CrossCompIntermediatesDir"
    fi
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


generate_event_logging_sources()
{
    if [ $__SkipCoreCLR == 1 ]; then
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
        # Drop version.cpp file
        __versionSourceFile="$intermediatesForBuild/version.cpp"
        if [ $__SkipGenerateVersion == 0 ]; then
            pwd
            "$__ProjectRoot/run.sh" build -Project=$__ProjectDir/build.proj -generateHeaderUnix -NativeVersionSourceFile=$__versionSourceFile $__RunArgs $__UnprocessedBuildArgs
        else
            # Generate the dummy version.cpp, but only if it didn't exist to make sure we don't trigger unnecessary rebuild
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
        echo "Invoking \"$__ProjectRoot/src/pal/tools/gen-buildsys-clang.sh\" \"$__ProjectRoot\" $__ClangMajorVersion $__ClangMinorVersion $platformArch $__BuildType $__CodeCoverage $__IncludeTests $generator $extraCmakeArguments $__cmakeargs"
        "$__ProjectRoot/src/pal/tools/gen-buildsys-clang.sh" "$__ProjectRoot" $__ClangMajorVersion $__ClangMinorVersion $platformArch $__BuildType $__CodeCoverage $__IncludeTests $generator "$extraCmakeArguments" "$__cmakeargs"
        popd
    fi

    if [ ! -f "$intermediatesForBuild/$buildFile" ]; then
        echo "Failed to generate $message build project!"
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

    # Build
    if [ $__ConfigureOnly == 1 ]; then
        echo "Finish configuration & skipping $message build."
        return
    fi

    # Check that the makefiles were created.
    pushd "$intermediatesForBuild"

    echo "Executing $buildTool install -j $NumProc"

    $buildTool install -j $NumProc
    if [ $? != 0 ]; then
        echo "Failed to build $message."
        exit 1
    fi

    popd
}

build_cross_arch_component()
{
    __SkipCrossArchBuild=1
    TARGET_ROOTFS=""
    # check supported cross-architecture components host(__HostArch)/target(__BuildArch) pair
    if [[ "$__BuildArch" == "arm" && "$__CrossArch" == "x86" ]]; then
        export CROSSCOMPILE=0
        __SkipCrossArchBuild=0

        # building x64-host/arm-target cross-architecture component need to use cross toolchain of x86
        if [ "$__HostArch" == "x64" ]; then
            export CROSSCOMPILE=1
        fi
    else
        # not supported
        return
    fi    
    
    export __CMakeBinDir="$__CrossComponentBinDir"
    export CROSSCOMPONENT=1
    __IncludeTests=

    if [ $CROSSCOMPILE == 1 ]; then
        TARGET_ROOTFS="$ROOTFS_DIR"
        if [ -n "$CAC_ROOTFS_DIR" ]; then
            export ROOTFS_DIR="$CAC_ROOTFS_DIR"
        else
            export ROOTFS_DIR="$__ProjectRoot/cross/rootfs/$__CrossArch"
        fi
    fi

    __ExtraCmakeArgs="-DCLR_CMAKE_TARGET_ARCH=$__BuildArch -DCLR_CMAKE_TARGET_OS=$__BuildOS -DCLR_CMAKE_PACKAGES_DIR=$__PackagesDir -DCLR_CMAKE_PGO_INSTRUMENT=$__PgoInstrument"
    build_native $__SkipCrossArchBuild "$__CrossArch" "$__CrossCompIntermediatesDir" "$__ExtraCmakeArgs" "cross-architecture component"
   
    # restore ROOTFS_DIR, CROSSCOMPONENT, and CROSSCOMPILE 
    if [ -n "$TARGET_ROOTFS" ]; then
        export ROOTFS_DIR="$TARGET_ROOTFS"
    fi
    export CROSSCOMPONENT=
    export CROSSCOMPILE=1
}

isMSBuildOnNETCoreSupported()
{
    # This needs to be updated alongwith corresponding changes to netci.groovy.
    __isMSBuildOnNETCoreSupported=0

    if [ "$__HostArch" == "x64" ]; then
        if [ "$__HostOS" == "Linux" ]; then
            case "$__HostDistroRid" in
                "centos.7-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "debian.8-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "fedora.23-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "fedora.24-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "opensuse.42.1-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "rhel.7"*"-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "ubuntu.14.04-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "ubuntu.16.04-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "ubuntu.16.10-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                "alpine.3.4.3-x64")
                    __isMSBuildOnNETCoreSupported=1
                    ;;
                *)
                __isMSBuildOnNETCoreSupported=$__msbuildonunsupportedplatform
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

    echo "Commencing build of managed components for $__BuildOS.$__BuildArch.$__BuildType"

    # Invoke MSBuild
    $__ProjectRoot/run.sh build -Project=$__ProjectDir/build.proj -MsBuildLog="/flp:Verbosity=normal;LogFile=$__LogsDir/System.Private.CoreLib_$__BuildOS__$__BuildArch__$__BuildType.log" -BuildTarget -__IntermediatesDir=$__IntermediatesDir -__RootBinDir=$__RootBinDir -BuildNugetPackage=false -UseSharedCompilation=false $__RunArgs $__UnprocessedBuildArgs

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

    echo "Generating nuget packages for "$__BuildOS

    # Build the packages
    $__ProjectRoot/run.sh build -Project=$__SourceDir/.nuget/packages.builds -MsBuildLog="/flp:Verbosity=normal;LogFile=$__LogsDir/Nuget_$__BuildOS__$__BuildArch__$__BuildType.log" -BuildTarget -__IntermediatesDir=$__IntermediatesDir -__RootBinDir=$__RootBinDir -BuildNugetPackage=false -UseSharedCompilation=false $__RunArgs $__UnprocessedBuildArgs

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
__RunArgs=
__MSBCleanBuildArgs=
__UseNinja=0
__VerboseBuild=0
__PgoInstrument=0
__ConfigureOnly=0
__SkipConfigure=0
__SkipRestore=""
__SkipNuget=0
__SkipCoreCLR=0
__SkipMSCorLib=0
__CrossBuild=0
__ClangMajorVersion=0
__ClangMinorVersion=0
__NuGetPath="$__PackagesDir/NuGet.exe"
__HostDistroRid=""
__DistroRid=""
__cmakeargs=""
__SkipGenerateVersion=0
__DoCrossArchBuild=0
__PortableLinux=0
__msbuildonunsupportedplatform=0

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

        armel)
            __BuildArch=armel
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

        cross)
            __CrossBuild=1
            ;;
            
		portablelinux)
            if [ "$__BuildOS" == "Linux" ]; then
                __PortableLinux=1
            else
                echo "ERROR: portableLinux not supported for non-Linux platforms."
                exit 1
            fi
            ;;
            
        verbose)
        __VerboseBuild=1
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

        clang3.9)
            __ClangMajorVersion=3
            __ClangMinorVersion=9
            ;;

        ninja)
            __UseNinja=1
            ;;

        pgoinstrument)
            __PgoInstrument=1
            ;;

        configureonly)
            __ConfigureOnly=1
            __SkipMSCorLib=1
            __SkipNuget=1
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

        crosscomponent)
            __DoCrossArchBuild=1
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

        skipnuget)
            __SkipNuget=1
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
        buildstandalonegc)
            __cmakeargs="-DFEATURE_STANDALONE_GC=1"
            ;;
        msbuildonunsupportedplatform)
            __msbuildonunsupportedplatform=1
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac

    shift
done

__RunArgs="-BuildArch=$__BuildArch -BuildType=$__BuildType -BuildOS=$__BuildOS"

# Configure environment if we are doing a verbose build
if [ $__VerboseBuild == 1 ]; then
    export VERBOSE=1
	__RunArgs="$__RunArgs -verbose"
fi

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

# init the host distro name
initHostDistroRid

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/Product/$__BuildOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__ToolsDir="$__RootBinDir/tools"
__TestWorkingDir="$__RootBinDir/tests/$__BuildOS.$__BuildArch.$__BuildType"
export __IntermediatesDir="$__RootBinDir/obj/$__BuildOS.$__BuildArch.$__BuildType"
__TestIntermediatesDir="$__RootBinDir/tests/obj/$__BuildOS.$__BuildArch.$__BuildType"
__isMSBuildOnNETCoreSupported=0
__CrossComponentBinDir="$__BinDir"
__CrossCompIntermediatesDir="$__IntermediatesDir/crossgen"

__CrossArch="$__HostArch"
if [[ "$__HostArch" == "x64" && "$__BuildArch" == "arm" ]]; then
    __CrossArch="x86"
fi
if [ $__CrossBuild == 1 ]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossgenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__BuildOS.$BuildArch.$__BuildType.log"
__CrossgenExe="$__CrossComponentBinDir/crossgen"

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

# init the target distro name
initTargetDistroRid

# Make the directories necessary for build if they don't exist
setup_dirs

# Check prereqs.
check_prereqs

# Generate event logging infrastructure sources
generate_event_logging_sources

# Build the coreclr (native) components.
__ExtraCmakeArgs="-DCLR_CMAKE_TARGET_OS=$__BuildOS -DCLR_CMAKE_PACKAGES_DIR=$__PackagesDir -DCLR_CMAKE_PGO_INSTRUMENT=$__PgoInstrument"
build_native $__SkipCoreCLR "$__BuildArch" "$__IntermediatesDir" "$__ExtraCmakeArgs" "CoreCLR component"

# Build cross-architecture components
if [[ $__CrossBuild == 1 && $__DoCrossArchBuild == 1 ]]; then
    build_cross_arch_component
fi

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
