#!/usr/bin/env bash

# Why is this a separate script? Why not just invoke 'cmake' and 'make' in the C# build scripts themselves?
# I really don't know, but it doesn't work when I do that. Something about SIGCHLD not getting from clang to cmake or something.
#       -anurse

usage()
{
    echo "Usage: $0 --arch <Architecture> --rid <Runtime Identifier> [--xcompiler <Cross C++ Compiler>]"
    echo ""
    echo "Options:"
    echo "  --arch <Architecture>             Target Architecture (amd64, x86, arm)"
    echo "  --rid <Runtime Identifier>        Target Runtime Identifier"
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
    amd64)
        __define=-DCLI_CMAKE_PLATFORM_ARCH_AMD64=1
        ;;
    x86)
        __define=-DCLI_CMAKE_PLATFORM_ARCH_I386=1
        ;;
    arm)
        __define=-DCLI_CMAKE_PLATFORM_ARCH_ARM=1
        ;;
    *)
        echo "Unknown architecture $__build_arch"; exit 1
        ;;
esac
__cmake_defines="${__cmake_defines} ${__define}"


echo "Building Corehost from $DIR to $(pwd)"
if [ $__CrossBuild == 1 ]; then
    cmake "$DIR" -G "Unix Makefiles" $__cmake_defines -DCLI_CMAKE_RUNTIME_ID:STRING=$__runtime_id -DCMAKE_CXX_COMPILER="$__CrossCompiler"
else
    cmake "$DIR" -G "Unix Makefiles" $__cmake_defines -DCLI_CMAKE_RUNTIME_ID:STRING=$__runtime_id
fi
make
