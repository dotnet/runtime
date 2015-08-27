#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [clean] [verbose] [cross] [clangx.y] [skipmscorlib]"
    echo "BuildArch can be: x64, ARM"
    echo "BuildType can be: Debug, Release"
    echo "clean - optional argument to force a clean build."
    echo "verbose - optional argument to enable verbose build output."
    echo "clangx.y - optional argument to build using clang version x.y."
    echo "cross - optional argument to signify cross compilation,"
    echo "      - will use ROOTFS_DIR environment variable if set."
    echo "skipmscorlib - do not build mscorlib.dll even if mono is installed."

    exit 1
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

build_coreclr()
{
    # All set to commence the build

    echo "Commencing build of native components for $__BuildOS.$__BuildArch.$__BuildType"
    cd "$__IntermediatesDir"

    # Regenerate the CMake solution
    echo "Invoking cmake with arguments: \"$__ProjectRoot\" $__CMakeArgs"
    "$__ProjectRoot/src/pal/tools/gen-buildsys-clang.sh" "$__ProjectRoot" $__ClangMajorVersion $__ClangMinorVersion $__BuildArch $__CMakeArgs

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

    # Build CoreCLR

    echo "Executing make install -j $NumProc $__UnprocessedBuildArgs"

    make install -j $NumProc $__UnprocessedBuildArgs
    if [ $? != 0 ]; then
        echo "Failed to build coreclr components."
        exit 1
    fi
}

build_mscorlib()
{
    hash mono 2> /dev/null || { echo >&2 "Skipping mscorlib.dll build since Mono is not installed."; return; }

    if [ $__SkipMSCorLib == 1 ]; then
        echo "Skipping mscorlib.dll build."
        return
    fi

    echo "Commencing build of mscorlib components for $__BuildOS.$__BuildArch.$__BuildType"

    # Pull NuGet.exe down if we don't have it already
    if [ ! -e "$__NuGetPath" ]; then
        hash curl 2>/dev/null || hash wget 2>/dev/null || { echo >&2 echo "cURL or wget is required to build mscorlib." ; exit 1; }

        echo "Restoring NuGet.exe..."

        # curl has HTTPS CA trust-issues less often than wget, so lets try that first.
        which curl > /dev/null 2> /dev/null
        if [ $? -ne 0 ]; then
           mkdir -p $__PackagesDir
           wget -q -O $__NuGetPath https://api.nuget.org/downloads/nuget.exe
        else
           curl -sSL --create-dirs -o $__NuGetPath https://api.nuget.org/downloads/nuget.exe
        fi

        if [ $? -ne 0 ]; then
            echo "Failed to restore NuGet.exe."
            exit 1
        fi
    fi

    # Grab the MSBuild package if we don't have it already
    if [ ! -e "$__MSBuildPath" ]; then
        echo "Restoring MSBuild..."
        mono "$__NuGetPath" install $__MSBuildPackageId -Version $__MSBuildPackageVersion -source "https://www.myget.org/F/dotnet-buildtools/" -OutputDirectory "$__PackagesDir"
        if [ $? -ne 0 ]; then
            echo "Failed to restore MSBuild."
            exit 1
        fi
    fi

    # Invoke MSBuild
    mono "$__MSBuildPath" /nologo "$__ProjectRoot/build.proj" /verbosity:minimal "/fileloggerparameters:Verbosity=normal;LogFile=$__LogsDir/MSCorLib_$__BuildOS__$__BuildArch__$__BuildType.log" /t:Build /p:OSGroup=$__BuildOS /p:BuildOS=$__BuildOS /p:BuildArch=$__MSBuildBuildArch /p:UseRoslynCompiler=true /p:BuildNugetPackage=false
}

echo "Commencing CoreCLR Repo build"

# Argument types supported by this script:
#
# Build architecture - valid values are: x64, ARM.
# Build Type         - valid values are: Debug, Release
#
# Set the default arguments for build

# Obtain the location of the bash script to figure out whether the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
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
__MSBuildBuildArch=x64
__BuildType=Debug
__CMakeArgs=DEBUG

# Set the various build properties here so that CMake and MSBuild can pick them up
__ProjectDir="$__ProjectRoot"
__SourceDir="$__ProjectDir/src"
__PackagesDir="$__ProjectDir/packages"
__RootBinDir="$__ProjectDir/bin"
__LogsDir="$__RootBinDir/Logs"
__UnprocessedBuildArgs=
__MSBCleanBuildArgs=
__SkipMSCorLib=false
__CleanBuild=false
__VerboseBuild=false
__CrossBuild=false
__ClangMajorVersion=3
__ClangMinorVersion=5
__MSBuildPackageId="Microsoft.Build.Mono.Debug"
__MSBuildPackageVersion="14.1.0.0-prerelease"
__MSBuildPath="$__PackagesDir/$__MSBuildPackageId.$__MSBuildPackageVersion/lib/MSBuild.exe"
__NuGetPath="$__PackagesDir/NuGet.exe"

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
        arm64)
        __BuildArch=arm64
        __MSBuildBuildArch=arm64
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
        skipmscorlib)
        __SkipMSCorLib=1
        ;;
        *)
        __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
    esac
done

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/Product/$__BuildOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__ToolsDir="$__RootBinDir/tools"
__TestWorkingDir="$__RootBinDir/tests/$__BuildOS.$__BuildArch.$__BuildType"
__IntermediatesDir="$__RootBinDir/obj/$__BuildOS.$__BuildArch.$__BuildType"
__TestIntermediatesDir="$__RootBinDir/tests/obj/$__BuildOS.$__BuildArch.$__BuildType"

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

# Build mscolrib.

build_mscorlib

# Build complete

echo "Repo successfully built."
echo "Product binaries are available at $__BinDir"
exit 0
