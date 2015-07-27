#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [clean] [verbose] [clangx.y]"
    echo "BuildArch can be: x64, arm"
    echo "BuildType can be: Debug, Release"
    echo "clean - optional argument to force a clean build."
    echo "verbose - optional argument to enable verbose build output."
    echo "clangx.y - optional argument to build using clang version x.y."

    exit 1
}

setup_dirs()
{
    echo Setting up directories for build
    
    mkdir -p "$__BinDir"
    mkdir -p "$__IntermediatesDir"
}

# Performs "clean build" type actions (deleting and remaking directories)

clean()
{
    echo Cleaning previous output for the selected configuration
    rm -rf "$__BinDir"
    rm -rf "$__IntermediatesDir"
}

# Check the system to ensure the right pre-reqs are in place

check_prereqs()
{
    echo "Checking pre-requisites..."
    
    # Check presence of CMake on the path
    hash cmake 2>/dev/null || { echo >&2 "Please install cmake before running this script"; exit 1; }
    
    # Check for clang
    hash clang-$__ClangMajorVersion.$__ClangMinorVersion 2>/dev/null ||  hash clang$__ClangMajorVersion$__ClangMinorVersion 2>/dev/null ||  hash clang 2>/dev/null || { echo >&2 "Please install clang before running this script"; exit 1; }
}

build_corefx_native()
{
    # All set to commence the build
    
    echo "Commencing build of corefx native components for $__BuildOS.$__BuildArch.$__BuildType"
    cd "$__IntermediatesDir"
    
    # Regenerate the CMake solution
    echo "Invoking cmake with arguments: \"$__ScriptDir\" $__CMakeArgs"
    "$__ScriptDir/gen-buildsys-clang.sh" "$__ScriptDir" $__ClangMajorVersion $__ClangMinorVersion $__CMakeArgs
    
    # Check that the makefiles were created.
    
    if [ ! -f "$__IntermediatesDir/Makefile" ]; then
        echo "Failed to generate native component build project!"
        exit 1
    fi

    # Get the number of processors available to the scheduler
    # Other techniques such as `nproc` only get the number of
    # processors available to a single process.
    if [ `uname` = "FreeBSD" ]; then
        NumProc=`sysctl hw.ncpu | awk '{ print $2+1 }'`
    else
        NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
    fi

    # Build
    
    echo "Executing make install -j $NumProc $__UnprocessedBuildArgs"

    make install -j $NumProc $__UnprocessedBuildArgs
    if [ $? != 0 ]; then
        echo "Failed to build corefx native components."
        exit 1
    fi
}

echo "Commencing CoreFX Native build"

# Argument types supported by this script:
#
# Build architecture - valid values are: x64, arm.
# Build Type         - valid values are: Debug, Release
#
# Set the default arguments for build

# Obtain the location of the bash script to figure out whether the root of the repo is.

__ScriptDir="$( dirname "${BASH_SOURCE[0]}" )"
__ScriptDir="$( cd $__ScriptDir && pwd )"
__ProjectRoot="$( cd $__ScriptDir && cd ../.. && pwd )"
__BuildArch=x64
# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        ;;
esac
__BuildType=Debug
__CMakeArgs=DEBUG

# Set the various build properties here so that CMake and MSBuild can pick them up
__ProjectDir="$__ProjectRoot"
__SourceDir="$__ScriptDir"
__RootBinDir="$__ProjectDir/bin"
__UnprocessedBuildArgs=
__CleanBuild=false
__VerboseBuild=false
__ClangMajorVersion=3
__ClangMinorVersion=5

for i in "$@"
    do
        lowerI="$(echo $i | awk '{print tolower($0)}')"
        case $lowerI in
        -?|-h|--help)
            usage
            exit 1
            ;;
        x64)
            __BuildArch=x64
            __MSBuildBuildArch=x64
            ;;
        arm)
            __BuildArch=arm
            __MSBuildBuildArch=arm
            ;;
        debug)
            __BuildType=Debug
            ;;
        release)
            __BuildType=Release
            __CMakeArgs=RELEASE
            ;;
        clean)
            __CleanBuild=1
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
        *)
          __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
    esac
done

# Set the remaining variables based upon the determined build configuration
__IntermediatesDir="$__RootBinDir/obj/$__BuildOS.$__BuildArch.$__BuildType/Native"
__BinDir="$__RootBinDir/$__BuildOS.$__BuildArch.$__BuildType/Native"

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

# Make the directories necessary for build if they don't exist

setup_dirs

# Check prereqs.

check_prereqs

# Build the corefx native components.

build_corefx_native

# Build complete

echo "CoreFX native components successfully built."
echo "Product binaries are available at $__BinDir"
exit 0
