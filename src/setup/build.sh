#!/usr/bin/env bash

# Why is this a separate script? Why not just invoke 'cmake' and 'make' in the C# build scripts themselves?
# I really don't know, but it doesn't work when I do that. Something about SIGCHLD not getting from clang to cmake or something.
#       -anurse

init_distro_name_and_rid()
{
    # Detect Distro
    if [ "$(cat /etc/*-release | grep -cim1 ubuntu)" -eq 1 ]; then
        export __distro_name=ubuntu
        export __rid_plat=ubuntu.14.04
    elif [ "$(cat /etc/*-release | grep -cim1 centos)" -eq 1 ]; then
        export __distro_name=rhel
        export __rid_plat=centos.7
    elif [ "$(cat /etc/*-release | grep -cim1 rhel)" -eq 1 ]; then
        export __distro_name=rhel
        export __rid_plat=rhel.7
    elif [ "$(cat /etc/*-release | grep -cim1 debian)" -eq 1 ]; then
        export __distro_name=debian
        export __rid_plat=debian.8
    else
        export __distro_name=""
        export __rid_plat=
    fi
}

usage()
{
    echo "Usage: $0 --arch <Architecture> --rid <Runtime Identifier> --policyver <HostPolicy library version> [--xcompiler <Cross C++ Compiler>]"
    echo ""
    echo "Options:"
    echo "  --arch <Architecture>             Target Architecture (amd64, x86, arm)"
    echo "  --rid <Runtime Identifier>        Target Runtime Identifier"
    echo "  --policyver <HostPolicy version>  Version of the hostpolicy library"
    echo "  --xcompiler <Cross C++ Compiler>  Cross Compiler when the target is arm"
    echo "                                    e.g.) /usr/bin/arm-linux-gnueabi-g++-4.7"

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
__policy_ver=
__CrossBuild=0

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
        --policyver)
            shift
            __policy_ver=$1
            ;;
        --xcompiler)
            shift
            __CrossCompiler="$1"
            __CrossBuild=1
            ;;
        *)
        echo "Unknown argument to build.sh $1"; exit 1
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
    arm)
        __arch_define=-DCLI_CMAKE_PLATFORM_ARCH_ARM=1
        ;;
    *)
        echo "Unknown architecture $__build_arch"; exit 1
        ;;
esac
__cmake_defines="${__cmake_defines} ${__arch_define}"


__rid_plat=
if [ "$(uname -s)" == "Darwin" ]; then
    __rid_plat=osx.10.10
else
    init_distro_name_and_rid
fi

if [ -z $__rid_plat ]; then
    echo "Unknown base rid (eg.: osx.10.10, ubuntu.14.04) being targeted"
    exit -1
fi

__build_arch_lowcase=$(echo "$__build_arch" | tr '[:upper:]' '[:lower:]')
__base_rid=$__rid_plat-$__build_arch_lowcase

echo "Building Corehost from $DIR to $(pwd)"
set -x # turn on trace
if [ $__CrossBuild == 1 ]; then
    cmake "$DIR" -G "Unix Makefiles" $__cmake_defines -DCLI_CMAKE_RUNTIME_ID:STRING=$__runtime_id -DCLI_CMAKE_HOST_POLICY_VER:STRING=$__policy_ver -DCMAKE_CXX_COMPILER="$__CrossCompiler" -DCLI_CMAKE_PKG_RID:STRING=$__base_rid
else
    cmake "$DIR" -G "Unix Makefiles" $__cmake_defines -DCLI_CMAKE_RUNTIME_ID:STRING=$__runtime_id -DCLI_CMAKE_HOST_POLICY_VER:STRING=$__policy_ver -DCLI_CMAKE_PKG_RID:STRING=$__base_rid
fi
set +x # turn off trace
make
