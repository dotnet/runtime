#!/usr/bin/env bash

# Why is this a separate script? Why not just invoke 'cmake' and 'make' in the C# build scripts themselves?
# I really don't know, but it doesn't work when I do that. Something about SIGCHLD not getting from clang to cmake or something.
#       -anurse

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
                if [ "$__runtime_id" ==  "$ID.$VERSION_ID-$__build_arch" ]; then
                    export __rid_plat="$ID.$VERSION_ID"
                else
                    echo "ROOTFS_DIR and Target RID are different. Please use correct rootfs for cross compilation."
                    exit -1
                fi
            fi
        fi
    else
        if [ -e /etc/os-release ]; then
            source /etc/os-release

            if [[ "$ID" == "rhel" && $VERSION_ID = 7* ]]; then
                export __rid_plat="rhel.7"
            elif [[ "$ID" == "centos" && "$VERSION_ID" = "7" ]]; then
                export __rid_plat="rhel.7"
            else
                export __rid_plat="$ID.$VERSION_ID"
            fi
        else
            export __rid_plat=
        fi
    fi

    if [ $__linkPortable == 1 ]; then
        export __rid_plat="linux"
    fi
}

usage()
{
    echo "Usage: $0 --arch <Architecture> --rid <Runtime Identifier> --hostver <Dotnet exe version> --apphostver <app host exe version> --fxrver <HostFxr library version> --policyver <HostPolicy library version> --commithash <Git commit hash> [--xcompiler <Cross C++ Compiler>]"
    echo ""
    echo "Options:"
    echo "  --arch <Architecture>             Target Architecture (x64, x86, arm, armel)"
    echo "  --rid <Runtime Identifier>        Target Runtime Identifier"
    echo "  --hostver <Dotnet host version>   Version of the dotnet executable"
    echo "  --apphostver <app host version>   Version of the apphost executable"
    echo "  --fxrver <HostFxr version>        Version of the hostfxr library"
    echo "  --policyver <HostPolicy version>  Version of the hostpolicy library"
    echo "  --commithash <Git commit hash>   Current commit hash of the repo at build time"
    echo "  --portableLinux                      Optional argument to build native libraries portable over GLIBC based Linux distros."
    echo "  --cross                           Optional argument to signify cross compilation,"
    echo "                                    and use ROOTFS_DIR environment variable to find rootfs."

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

__build_arch=
__runtime_id=
__host_ver=
__apphost_ver=
__policy_ver=
__fxr_ver=
__CrossBuild=0
__commit_hash=
__linkPortable=0

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
        --rid) 
            shift
            __runtime_id=$1
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
        --portablelinux)
            __linkPortable=1
            ;;
        --cross)
            __CrossBuild=1
            ;;
        *)
        echo "Unknown argument to build.sh $1"; usage; exit 1
    esac
    shift
done

__cmake_defines=

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
    *)
        echo "Unknown architecture $__build_arch"; usage; exit 1
        ;;
esac
__cmake_defines="${__cmake_defines} ${__arch_define}"

# __rid_plat is the base RID that corehost is shipped for, effectively, the name of the folder in "runtimes/{__rid_plat}/native/" inside the nupkgs.
__rid_plat=
if [ "$(uname -s)" == "Darwin" ]; then
    __rid_plat=osx.10.10
else
    init_rid_plat
fi

if [ -z $__rid_plat ]; then
    echo "Unknown base rid (eg.: osx.10.10, ubuntu.14.04) being targeted"
    exit -1
fi

if [ -z $__commit_hash ]; then
    echo "Commit hash was not specified"
    exit -1
fi

__build_arch_lowcase=$(echo "$__build_arch" | tr '[:upper:]' '[:lower:]')
__base_rid=$__rid_plat-$__build_arch_lowcase

echo "Building Corehost from $DIR to $(pwd)"
set -x # turn on trace
if [ $__CrossBuild == 1 ]; then
    cmake "$DIR" -G "Unix Makefiles" $__cmake_defines -DCLI_CMAKE_RUNTIME_ID:STRING=$__runtime_id -DCLI_CMAKE_HOST_VER:STRING=$__host_ver -DCLI_CMAKE_APPHOST_VER:STRING=$__apphost_ver -DCLI_CMAKE_HOST_FXR_VER:STRING=$__fxr_ver -DCLI_CMAKE_HOST_POLICY_VER:STRING=$__policy_ver -DCLI_CMAKE_PKG_RID:STRING=$__base_rid -DCLI_CMAKE_COMMIT_HASH:STRING=$__commit_hash -DCMAKE_TOOLCHAIN_FILE=$DIR/../../cross/$__build_arch_lowcase/toolchain.cmake
else
    cmake "$DIR" -G "Unix Makefiles" $__cmake_defines -DCLI_CMAKE_RUNTIME_ID:STRING=$__runtime_id -DCLI_CMAKE_HOST_VER:STRING=$__host_ver -DCLI_CMAKE_APPHOST_VER:STRING=$__apphost_ver -DCLI_CMAKE_HOST_FXR_VER:STRING=$__fxr_ver -DCLI_CMAKE_HOST_POLICY_VER:STRING=$__policy_ver -DCLI_CMAKE_PKG_RID:STRING=$__base_rid -DCLI_CMAKE_COMMIT_HASH:STRING=$__commit_hash
fi
set +x # turn off trace
make
