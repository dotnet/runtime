#!/bin/bash

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [clean]"
    echo "BuildArch can be: amd64"
    echo "BuildType can be: debug, release"
    echo "clean - optional argument to force a clean build."

    exit 1
}

# Performs "clean build" type actions (deleting and remaking directories)

clean()
{
    echo Doing a clean build

    # make projects would need a rebuild
    MakeCleanArgs=clean

    # Cleanup the binaries drop folder
    if [ -d "$__BinDir" ]; then 
        rm -r "$__BinDir"
    fi
    
    mkdir -p "$__BinDir"

    # Cleanup the CMake folder
    if [ -d "$__CMakeSlnDir" ]; then
         rm -r "$__CMakeSlnDir"
    fi
    mkdir -p "$__CMakeSlnDir"

    # Cleanup the logs folder
    if [ -d "$__LogsDir" ]; then
        rm -r "$__LogsDir"
    fi
    
    mkdir -p "$__LogsDir"

    # Cleanup intermediates folder
    if [ -d "$__IntermediatesDir" ]; then
        rm -r "$__IntermediatesDir"
    fi

    mkdir -p "$__IntermediatesDir"
}

# Check the system to ensure the right pre-reqs are in place

check_prereqs()
{
    echo "Checking pre-requisites..."
    
    # Check presence of CMake on the path
    hash cmake 2>/dev/null || { echo >&2 "Please install cmake before running this script"; exit 1; }
    
    # Check for clang
    hash clang 2>/dev/null || { echo >&2 "Please install clang before running this script"; exit 1; }
}

build_coreclr()
{
    # All set to commence the build
    
    echo "Commencing build of native components for $__BuildArch/$__BuildType"
    cd "$__CMakeSlnDir"
    
    # Regenerate the CMake solution
    echo "Invoking cmake with arguments: \"$__ProjectRoot\" $__CMakeArgs"
    "$__ProjectRoot/src/pal/tools/gen-buildsys-clang.sh" "$__ProjectRoot" $__CMakeArgs
    
    # Check that the makefiles were created.
    
    if [ ! -f "$__CMakeSlnDir/Makefile" ]; then
        echo "Failed to generate native component build project!"
        exit 1
    fi

    # Get the number of processors available to the scheduler
    # Other techniques such as `nproc` only get the number of
    # processors available to a single process.
    NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
    
    # Build CoreCLR
    
    echo "Executing make install -j $NumProc $__UnprocessedBuildArgs"

    make install -j $NumProc $__UnprocessedBuildArgs
    if [ $? != 0 ]; then
        echo "Failed to build coreclr components."
        exit 1
    fi
}

echo "Commencing CoreCLR Repo build"

# Argument types supported by this script:
#
# Build architecture - valid value is: Amd64.
# Build Type         - valid values are: Debug, Release
#
# Set the default arguments for build

# Obtain the location of the bash script to figure out whether the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
__BuildArch=amd64
__MSBuildBuildArch=x64
__BuildType=debug
__CMakeArgs=DEBUG

# Set the various build properties here so that CMake and MSBuild can pick them up
__ProjectDir="$__ProjectRoot"
__SourceDir="$__ProjectDir/src"
__PackagesDir="$__SourceDir/.nuget"
__RootBinDir="$__ProjectDir/binaries"
__LogsDir="$__RootBinDir/Logs"
__CMakeSlnDir="$__RootBinDir/CMake"
__UnprocessedBuildArgs=
__MSBCleanBuildArgs=
__CleanBuild=false

for i in "$@"
    do
        lowerI="$(echo $i | awk '{print tolower($0)}')"
        case $lowerI in
        -?|-h|--help)
        usage
        exit 1
        ;;
        amd64)
        __BuildArch=amd64
        __MSBuildBuildArch=x64
        ;;
        debug)
        __BuildType=debug
        ;;
        release)
        __BuildType=release
        __CMakeArgs=RELEASE
        ;;
        clean)
        __CleanBuild=1
        ;;
        *)
        __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
    esac
done

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/Product/$__BuildArch/$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__ToolsDir="$__RootBinDir/tools"
__TestWorkingDir="$__RootBinDir/tests/$__BuildArch/$__BuildType"
__IntermediatesDir="$__RootBinDir/intermediates/$__BuildArch/$__BuildType"

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built CoreClr libraries will copied to.
export __CMakeBinDir="$__BinDir"

# Switch to clean build mode if the binaries output folder does not exist
if [ ! -d "$__RootBinDir" ]; then
    __CleanBuild=1
fi

# Configure environment if we are doing a clean build.
if [ $__CleanBuild == 1 ]; then
    clean
fi

# Check prereqs.

check_prereqs

# Build the coreclr (native) components.

build_coreclr

# Build complete

echo "Repo successfully built."
echo "Product binaries are available at $__BinDir"
exit 0
