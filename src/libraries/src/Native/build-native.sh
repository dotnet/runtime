#!/usr/bin/env bash

usage()
{
    echo "Our parameters changed! The parameters: buildArch, buildType, buildOS, numProc"
    echo "are passed by the Run Command Tool."
    echo "If you plan to only run this script, be sure to pass those parameters."
    echo "For more information type build-native.sh -? at the root of the repo."
    echo
    echo "Usage: $0 [runParameters][-verbose] [-clangx.y] [-gccx.y] [-cross] [-staticLibLink] [-cmakeargs] [-makeargs]"
    echo "runParameters: buildArch, buildType, buildOS, -numProc <numproc value>"
    echo "BuildArch can be: -x64, -x86, -arm, -armel, -arm64"
    echo "BuildType can be: -debug, -checked, -release"
    echo "-verbose - optional argument to enable verbose build output."
    echo "-clangx.y - optional argument to build using clang version x.y."
    echo "-gccx.y - optional argument to build using gcc version x.y."
    echo "-cross - optional argument to signify cross compilation,"
    echo "       - will use ROOTFS_DIR environment variable if set."
    echo "-staticLibLink - Optional argument to statically link any native library."
    echo "-portable - Optional argument to build native libraries portable over GLIBC based Linux distros."
    echo "-stripSymbols - Optional argument to strip native symbols during the build."
    echo "-skipgenerateversion - Pass this in to skip getting a version on the build output."
    echo "-cmakeargs - user-settable additional arguments passed to CMake."
    exit 1
}

initHostDistroRid()
{
    __HostDistroRid=""
    if [ "$__HostOS" == "Linux" ]; then
        if [ -e /etc/os-release ]; then
            source /etc/os-release
            if [[ $ID == "alpine" ]]; then
                # remove the last version digit
                VERSION_ID=${VERSION_ID%.*}
            fi
            __HostDistroRid="$ID.$VERSION_ID-$__HostArch"
        elif [ -e /etc/redhat-release ]; then
            local redhatRelease=$(</etc/redhat-release)
            if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]; then
               __HostDistroRid="rhel.6-$__HostArch"
            fi
        fi
    elif  [ "$__HostOS" == "OSX" ]; then
        _osx_version=`sw_vers -productVersion | cut -f1-2 -d'.'`
        __HostDistroRid="osx.$_osx_version-x64"
    elif [ "$__HostOS" == "FreeBSD" ]; then
      __freebsd_version=`sysctl -n kern.osrelease | cut -f1 -d'-'`
      __HostDistroRid="freebsd.$__freebsd_version-x64"
    fi


    if [ "$__HostDistroRid" == "" ]; then
        echo "WARNING: Can not determine runtime id for current distro."
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
}

setup_dirs()
{
    echo Setting up directories for build

    mkdir -p "$__BinDir"
    mkdir -p "$__IntermediatesDir"
}

# Check the system to ensure the right pre-reqs are in place
check_native_prereqs()
{
    echo "Checking pre-requisites..."

    # Check presence of CMake on the path
    hash cmake 2>/dev/null || { echo >&2 "Please install cmake before running this script"; exit 1; }

    if [ "$__GccBuild" = 0 ]; then
        # Minimum required version of clang is version 3.9 for arm/armel cross build
        if [ "$__CrossBuild" = 1 ] && { [ "$__BuildArch" = "arm" ] || [ "$__BuildArch" = "armel" ]; }; then
            if [ "$__ClangMajorVersion" -lt 3 ] || { [ "$__ClangMajorVersion" -eq 3 ] && [ "$__ClangMinorVersion" -lt 9 ]; }; then
                echo "Please install clang3.9 or latest for arm/armel cross build"; exit 1;
            fi
        fi

        # Check for clang
        hash "clang-$__ClangMajorVersion.$__ClangMinorVersion" 2>/dev/null || hash "clang$__ClangMajorVersion$__ClangMinorVersion" 2>/dev/null ||  hash clang 2>/dev/null || { echo >&2 "Please install clang before running this script"; exit 1; }
    else
        # Minimum required version of gcc is version 5.0 for arm/armel cross build
        if [ "$__CrossBuild" = 1 ] && { [ "$__BuildArch" = "arm" ] || [ "$__BuildArch" = "armel" ]; }; then
            if [ "$__GccMajorVersion" -lt 5 ]; then
                echo "Please install gcc version 5 or latest for arm/armel cross build"; exit 1;
            fi
        fi

        # Check for gcc
        hash "gcc-$__GccMajorVersion.$__GccMinorVersion" 2>/dev/null || hash "gcc$__GccMajorVersion$__GccMinorVersion" 2>/dev/null ||  hash gcc 2>/dev/null || { echo >&2 "Please install gcc before running this script"; exit 1; }
    fi
}

prepare_native_build()
{
    # Specify path to be set for CMAKE_INSTALL_PREFIX.
    # This is where all built CoreClr libraries will copied to.
    export __CMakeBinDir="$__BinDir"

    # Configure environment if we are doing a verbose build
    if [ $__VerboseBuild == 1 ]; then
        export VERBOSE=1
    fi

    # Generate version.c if specified, else have an empty one.
    __versionSourceFile=$__artifactsDir/obj/_version.c
    if [ ! -e "${__versionSourceFile}" ]; then
        __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
        echo "${__versionSourceLine}" > ${__versionSourceFile}
    fi
}

build_native()
{
    # All set to commence the build

    echo "Commencing build of corefx native components for $__BuildOS.$__BuildArch.$__BuildType"
    cd "$__IntermediatesDir"

    # Regenerate the CMake solution
    if [ "$__GccBuild" = 0 ]; then
        echo "Invoking \"$__nativeroot/gen-buildsys-clang.sh\" \"$__nativeroot\" \"$__ClangMajorVersion\" \"$__ClangMinorVersion\" \"$__BuildArch\" \"$__CMakeArgs\" \"$__CMakeExtraArgs\""
        "$__nativeroot/gen-buildsys-clang.sh" "$__nativeroot" "$__ClangMajorVersion" "$__ClangMinorVersion" "$__BuildArch" "$__CMakeArgs" "$__CMakeExtraArgs"
    else
        echo "Invoking \"$__nativeroot/gen-buildsys-gcc.sh\" \"$__nativeroot\" \"$__GccMajorVersion\" \"$__GccMinorVersion\" \"$__BuildArch\" \"$__CMakeArgs\" \"$__CMakeExtraArgs\""
        "$__nativeroot/gen-buildsys-gcc.sh" "$__nativeroot" "$__GccMajorVersion" "$__GccMinorVersion" "$__BuildArch" "$__CMakeArgs" "$__CMakeExtraArgs"
    fi

    # Check that the makefiles were created.

    if [ ! -f "$__IntermediatesDir/Makefile" ]; then
        echo "Failed to generate native component build project!"
        exit 1
    fi

    # Build

    echo "Executing make install -j $__NumProc $__MakeExtraArgs"

    make install -j $__NumProc $__MakeExtraArgs
    if [ $? != 0 ]; then
        echo "Failed to build corefx native components."
        exit 1
    fi
}

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__nativeroot=$__scriptpath/Unix
__rootRepo="$__scriptpath/../.."
__artifactsDir="$__rootRepo/artifacts"

# Set the various build properties here so that CMake and MSBuild can pick them up
__CMakeExtraArgs=""
__MakeExtraArgs=""
__BuildArch=x64
__BuildType=Debug
__CMakeArgs=DEBUG
__BuildOS=Linux
__NumProc=1
__UnprocessedBuildArgs=
__GccBuild=0
__GccMajorVersion=0
__GccMinorVersion=0
__CrossBuild=0
__ServerGC=0
__VerboseBuild=false
__ClangMajorVersion=0
__ClangMinorVersion=0
__StaticLibLink=0
__PortableBuild=0

CPUName=$(uname -m)
if [ "$CPUName" == "i686" ]; then
    __BuildArch=x86
fi

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
        release|-release)
            __BuildType=Release
            __CMakeArgs=RELEASE
            ;;
        outconfig|-outconfig)
            __outConfig=$2
            shift
            ;;
        freebsd|FreeBSD|-freebsd|-FreeBSD)
            __BuildOS=FreeBSD
            ;;
        linux|-linux)
            __BuildOS=Linux
            ;;
        netbsd|-netbsd)
            __BuildOS=NetBSD
            ;;
        osx|-osx)
            __BuildOS=OSX
            ;;
        stripsymbols|-stripsymbols)
            __CMakeExtraArgs="$__CMakeExtraArgs -DSTRIP_SYMBOLS=true"
            ;;
        --numproc|-numproc|numproc)
            shift
            __NumProc=$1
            ;;
        verbose|-verbose)
            __VerboseBuild=1
            ;;
        staticliblink|-staticliblink)
            __StaticLibLink=1
            ;;
        -portable|-portable)
            # Portable native components are only supported on Linux
            if [ "$__HostOS" == "Linux" ]; then
                __PortableBuild=1
            fi
            ;;
        --clang*)
                # clangx.y or clang-x.y
                v=`echo $lowerI | tr -d '[:alpha:]-='`
                __ClangMajorVersion=`echo $v | cut -d '.' -f1`
                __ClangMinorVersion=`echo $v | cut -d '.' -f2`
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
        cross|-cross)
            __CrossBuild=1
            ;;
        cmakeargs|-cmakeargs)
            if [ -n "$2" ]; then
                __CMakeExtraArgs="$__CMakeExtraArgs $2"
                shift
            else
                echo "ERROR: 'cmakeargs' requires a non-empty option argument"
                exit 1
            fi
            ;;
        makeargs|-makeargs)
            if [ -n "$2" ]; then
                __MakeExtraArgs="$__MakeExtraArgs $2"
                shift
            else
                echo "ERROR: 'makeargs' requires a non-empty option argument"
                exit 1
            fi
            ;;
        useservergc|-useservergc)
            __ServerGC=1
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
    esac

    shift
done

__CMakeExtraArgs="$__CMakeExtraArgs -DFEATURE_DISTRO_AGNOSTIC_SSL=$__PortableBuild"
__CMakeExtraArgs="$__CMakeExtraArgs -DCMAKE_STATIC_LIB_LINK=$__StaticLibLink"

# Set cross build
case $CPUName in
    i686)
        if [ $__BuildArch != x86 ]; then
            __CrossBuild=1
            echo "Set CrossBuild for $__BuildArch build"
        fi
        ;;
    x86_64)
        if [ $__BuildArch != x64 ]; then
            __CrossBuild=1
            echo "Set CrossBuild for $__BuildArch build"
        fi
        ;;
esac

# Set the default clang version if not already set
if [[ $__ClangMajorVersion == 0 && $__ClangMinorVersion == 0 ]]; then
    __ClangMajorVersion=3
    __ClangMinorVersion=9
fi

# Set the remaining variables based upon the determined build configuration
__outConfig=${__outConfig:-"$__BuildOS-$__BuildArch-$__BuildType"}
__IntermediatesDir="$__artifactsDir/obj/native/$__outConfig"
__BinDir="$__artifactsDir/bin/native/$__outConfig"

# Make the directories necessary for build if they don't exist
setup_dirs

# Configure environment if we are doing a cross compile.
if [ "$__CrossBuild" == 1 ]; then
    export CROSSCOMPILE=1
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        export ROOTFS_DIR="$__rootRepo/cross/rootfs/$__BuildArch"
    fi
fi

# init the host distro name
initHostDistroRid

# init the target distro name
initTargetDistroRid

    # Check prereqs.

    check_native_prereqs

    # Prepare the system

    prepare_native_build

    # Build the corefx native components.

    build_native
