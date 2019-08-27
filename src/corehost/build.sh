#!/usr/bin/env bash

init_rid_plat()
{
    # Detect Distro
    if [ $__CrossBuild == 1 ]; then
        if [ -z $ROOTFS_DIR ]; then
            echo "ROOTFS_DIR is not defined."
            exit -1
        else
            if [ -e $ROOTFS_DIR/etc/os-release ]; then
                source $ROOTFS_DIR/etc/os-release
                __rid_plat="$ID.$VERSION_ID"
                if [[ "$ID" == "alpine" ]]; then
                    __rid_plat="linux-musl"
                fi
            fi
            echo "__rid_plat is $__rid_plat"
        fi
    else
        __rid_plat=""
        if [ -e /etc/os-release ]; then
            source /etc/os-release
            if [[ "$ID" == "rhel" ]]; then
                # remove the last version number
                VERSION_ID=${VERSION_ID%.*}
            fi
            __rid_plat="$ID.$VERSION_ID"
            if [[ "$ID" == "alpine" ]]; then
                __rid_plat="linux-musl"
            fi
        elif [ -e /etc/redhat-release ]; then
            local redhatRelease=$(</etc/redhat-release)
            if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]; then
               __rid_plat="rhel.6"
            fi
        fi
    fi

    if [ "$(uname -s)" == "Darwin" ]; then
        __rid_plat=osx.10.12
    fi
    if [ "$(uname -s)" == "FreeBSD" ]; then
        major_ver=`uname -U | cut -b1-2`
        __rid_plat=freebsd.$major_ver
    fi

    if [ $__linkPortable == 1 ]; then
        if [ "$(uname -s)" == "Darwin" ]; then
            __rid_plat="osx"
        elif [ "$(uname -s)" == "FreeBSD" ]; then
            __rid_plat="freebsd"
        else
            __rid_plat="linux"
        fi
    fi
}

usage()
{
    echo "Usage: $0 --configuration <configuration> --arch <Architecture> --hostver <Dotnet exe version> --apphostver <app host exe version> --fxrver <HostFxr library version> --policyver <HostPolicy library version> --commithash <Git commit hash> [--xcompiler <Cross C++ Compiler>]"
    echo ""
    echo "Options:"
    echo "  --configuration <configuration>   Build configuration (Debug, Release)"
    echo "  --arch <Architecture>             Target Architecture (x64, x86, arm, arm64, armel)"
    echo "  --hostver <Dotnet host version>   Version of the dotnet executable"
    echo "  --apphostver <app host version>   Version of the apphost executable"
    echo "  --fxrver <HostFxr version>        Version of the hostfxr library"
    echo "  --policyver <HostPolicy version>  Version of the hostpolicy library"
    echo "  --commithash <Git commit hash>    Current commit hash of the repo at build time"
    echo "  -portable                         Optional argument to build portable platform packages."
    echo "  --cross                           Optional argument to signify cross compilation,"
    echo "                                    and use ROOTFS_DIR environment variable to find rootfs."
    echo "  --stripsymbols                    Optional argument to strip native symbols during the build"

    exit 1
}

set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
RootRepo="$DIR/../.."

__bin_dir="$RootRepo/artifacts/bin"
__build_arch=
__host_ver=
__apphost_ver=
__policy_ver=
__fxr_ver=
__CrossBuild=0
__commit_hash=
__portableBuildArgs=
__configuration=Debug
__linkPortable=0
__cmake_defines=
__baseIntermediateOutputPath="$RootRepo/artifacts/obj"
__versionSourceFile="$__baseIntermediateOutputPath/_version.c"
__cmake_bin_prefix=

while [ "$1" != "" ]; do
        lowerI="$(echo $1 | awk '{print tolower($0)}')"
        case $lowerI in
        -h|--help)
            usage
            exit 1
            ;;
        --arch)
            shift
            __build_arch=$1
            ;;
        --configuration)
            shift
            __configuration=$1
            ;;
        --hostver)
            shift
            __host_ver=$1
            ;;
        --apphostver)
            shift
            __apphost_ver=$1
            ;;
        --fxrver)
            shift
            __fxr_ver=$1
            ;;
        --policyver)
            shift
            __policy_ver=$1
            ;;
        --commithash)
            shift
            __commit_hash=$1
            ;;
        -portable)
            __portableBuildArgs="-DCLI_CMAKE_PORTABLE_BUILD=1"
            __linkPortable=1
            ;;
        --cross)
            __CrossBuild=1
            ;;
        --stripsymbols)
            __cmake_defines="${__cmake_defines} -DSTRIP_SYMBOLS=true"
            ;;
        *)
        echo "Unknown argument to build.sh $1"; usage; exit 1
    esac
    shift
done

__cmake_defines="${__cmake_defines} -DCMAKE_BUILD_TYPE=${__configuration} ${__portableBuildArgs}"

mkdir -p "$__baseIntermediateOutputPath"

case $__build_arch in
    amd64|x64)
        __arch_define=-DCLI_CMAKE_PLATFORM_ARCH_AMD64=1
        ;;
    x86)
        __arch_define=-DCLI_CMAKE_PLATFORM_ARCH_I386=1
        ;;
    arm|armel)
        __arch_define=-DCLI_CMAKE_PLATFORM_ARCH_ARM=1
        ;;
    arm64)
        __arch_define=-DCLI_CMAKE_PLATFORM_ARCH_ARM64=1
        ;;
    *)
        echo "Unknown architecture $__build_arch"; usage; exit 1
        ;;
esac
__cmake_defines="${__cmake_defines} ${__arch_define}"

# Configure environment if we are doing a cross compile.
if [ "$__CrossBuild" == 1 ]; then
    if ! [[ -n $ROOTFS_DIR ]]; then
        export ROOTFS_DIR="$RootRepo/cross/rootfs/$__build_arch"
    fi
fi

# __base_rid is the base RID that corehost is shipped for, effectively, the name of the folder in "runtimes/{__base_rid}/native/" inside the nupkgs.
# __rid_plat is the OS portion of the RID.
__rid_plat=
init_rid_plat

if [ -z $__rid_plat ]; then
    echo "Unknown base rid (eg.: osx.10.12, ubuntu.14.04) being targeted"
    exit -1
fi

if [ -z $__commit_hash ]; then
    echo "Commit hash was not specified"
    exit -1
fi

__build_arch_lowcase=$(echo "$__build_arch" | tr '[:upper:]' '[:lower:]')
__base_rid=$__rid_plat-$__build_arch_lowcase
echo "Computed RID for native build is $__base_rid"
__cmake_bin_prefix="$__bin_dir/$__base_rid.$__configuration"
__intermediateOutputPath="$__baseIntermediateOutputPath/$__base_rid.$__configuration/corehost"
export __CrossToolChainTargetRID=$__base_rid

# Set up the environment to be used for building with clang.
if command -v "clang-3.5" > /dev/null 2>&1; then
    export CC="$(command -v clang-3.5)"
    export CXX="$(command -v clang++-3.5)"
elif command -v "clang-3.6" > /dev/null 2>&1; then
    export CC="$(command -v clang-3.6)"
    export CXX="$(command -v clang++-3.6)"
elif command -v "clang-3.9" > /dev/null 2>&1; then
    export CC="$(command -v clang-3.9)"
    export CXX="$(command -v clang++-3.9)"
elif command -v "clang-5.0" > /dev/null 2>&1; then
    export CC="$(command -v clang-5.0)"
    export CXX="$(command -v clang++-5.0)"
elif command -v clang > /dev/null 2>&1; then
    export CC="$(command -v clang)"
    export CXX="$(command -v clang++)"
else
    echo "Unable to find Clang Compiler"
    echo "Install clang-3.5 or clang3.6 or clang3.9 or clang5.0"
    exit 1
fi

if [ ! -f $__versionSourceFile ]; then
    __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
    echo $__versionSourceLine > $__versionSourceFile
fi

__cmake_defines="${__cmake_defines} -DVERSION_FILE_PATH:STRING=${__versionSourceFile}"

mkdir -p $__intermediateOutputPath
pushd $__intermediateOutputPath

echo "Building Corehost from $DIR to $(pwd)"
set -x # turn on trace
if [ $__CrossBuild == 1 ]; then
    # clang-3.9 or clang-4.0 are default compilers for cross compilation
    if [[ "$__build_arch" != "arm" && "$__build_arch" != "armel" ]]; then
        if command -v "clang-3.9" > /dev/null 2>&1; then
            export CC="$(command -v clang-3.9)"
            export CXX="$(command -v clang++-3.9)"
        fi
    elif command -v "clang-4.0" > /dev/null 2>&1; then
        export CC="$(command -v clang-4.0)"
        export CXX="$(command -v clang++-4.0)"
    elif command -v "clang-5.0" > /dev/null 2>&1; then
        export CC="$(command -v clang-5.0)"
        export CXX="$(command -v clang++-5.0)"
    else
        echo "Unable to find Clang 3.9 or Clang 4.0 or Clang 5.0 Compiler"
        echo "Install clang-3.9 or clang-4.0 or clang-5.0 for cross compilation"
        exit 1
    fi
    export TARGET_BUILD_ARCH=$__build_arch_lowcase
    export __DistroRid=$__rid_plat
    cmake "$DIR" -G "Unix Makefiles" $__cmake_defines -DCLI_CMAKE_HOST_VER:STRING=$__host_ver -DCLI_CMAKE_COMMON_HOST_VER:STRING=$__apphost_ver -DCLI_CMAKE_HOST_FXR_VER:STRING=$__fxr_ver -DCLI_CMAKE_HOST_POLICY_VER:STRING=$__policy_ver -DCLI_CMAKE_PKG_RID:STRING=$__base_rid -DCLI_CMAKE_COMMIT_HASH:STRING=$__commit_hash -DCMAKE_INSTALL_PREFIX=$__cmake_bin_prefix -DCMAKE_TOOLCHAIN_FILE=$DIR/../../cross/toolchain.cmake
else
    cmake "$DIR" -G "Unix Makefiles" $__cmake_defines -DCLI_CMAKE_HOST_VER:STRING=$__host_ver -DCLI_CMAKE_COMMON_HOST_VER:STRING=$__apphost_ver -DCLI_CMAKE_HOST_FXR_VER:STRING=$__fxr_ver -DCLI_CMAKE_HOST_POLICY_VER:STRING=$__policy_ver -DCLI_CMAKE_PKG_RID:STRING=$__base_rid -DCLI_CMAKE_COMMIT_HASH:STRING=$__commit_hash -DCMAKE_INSTALL_PREFIX=$__cmake_bin_prefix
fi
popd

set +x # turn off trace
cmake --build $__intermediateOutputPath --target install --config $__configuration
