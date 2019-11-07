#!/usr/bin/env bash
#
# This file invokes cmake and generates the build system for Clang.
#

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

if [ $# -lt 5 -o $# -gt 7 ]
then
  echo "Usage..."
  echo "gen-buildsys-clang.sh <path to repo root> <path to top level CMakeLists.txt> <ClangMajorVersion> <ClangMinorVersion> <Architecture> [build flavor] [cmakeargs]"
  echo "Specify the path to the top level CMake file - <corefx>/src/Native/Unix"
  echo "Specify the clang version to use, split into major and minor version"
  echo "Specify the target architecture." 
  echo "Optionally specify the build configuration (flavor.) Defaults to DEBUG." 
  echo "Optionally pass additional arguments to CMake call."
  exit 1
fi

#root directory of the project
repo_root=$1

# Set up the environment to be used for building with clang.
if which "clang-$3.$4" > /dev/null 2>&1
    then
        export CC="$(which clang-$3.$4)"
elif which "clang-$3$4" > /dev/null 2>&1
    then
        export CC="$(which clang-$3$4)"
elif which "clang$3$4" > /dev/null 2>&1
    then
        export CC="$(which clang$3$4)"
elif which clang > /dev/null 2>&1
    then
        export CC="$(which clang)"
else
    echo "Unable to find Clang Compiler"
    exit 1
fi

build_arch="$5"
# Possible build types are DEBUG, RELEASE, RELWITHDEBINFO, MINSIZEREL.
# Default to DEBUG
if [ -z "$6" ]
then
  echo "Defaulting to DEBUG build."
  buildtype="DEBUG"
else
  buildtype="$6"
fi

cmake_cmd=cmake
cmake_extra_defines="-DCMAKE_BUILD_TYPE=$buildtype"
if [[ -n "$CROSSCOMPILE" ]]; then
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        echo "ROOTFS_DIR not set for crosscompile"
        exit 1
    fi
    if [[ -z "$CONFIG_DIR" ]]; then
        CONFIG_DIR="$repo_root/eng/common/cross"
    fi
    export TARGET_BUILD_ARCH=$build_arch
    cmake_extra_defines="$cmake_extra_defines -C $scriptroot/tryrun.cmake"
    cmake_extra_defines="$cmake_extra_defines -DCMAKE_TOOLCHAIN_FILE=$CONFIG_DIR/toolchain.cmake"
fi
if [ "$build_arch" == "armel" ]; then
    cmake_extra_defines="$cmake_extra_defines -DARM_SOFTFP=1"
fi
if [ "$build_arch" == "wasm" ]; then
   if [ "$EMSCRIPTEN_ROOT" == "" ]; then
       EMSCRIPTEN_ROOT="$EMSDK_PATH/upstream/emscripten"
   fi
   cmake_cmd="emcmake cmake"
   cmake_extra_defines="$cmake_extra_defines -DCMAKE_TOOLCHAIN_FILE=$EMSCRIPTEN_ROOT/cmake/Modules/Platform/Emscripten.cmake -DEMSCRIPTEN_GENERATE_BITCODE_STATIC_LIBRARIES=1"
fi

__UnprocessedCMakeArgs=""
if [ -z "$7" ]; then
    echo "No CMake extra Args specified"
else
    __UnprocessedCMakeArgs="$7"
fi

echo "Invoking \"$cmake_cmd $cmake_extra_defines $__UnprocessedCMakeArgs $2\""
$cmake_cmd $cmake_extra_defines \
    $__UnprocessedCMakeArgs \
    $2
