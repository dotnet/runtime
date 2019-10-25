#!/usr/bin/env bash
#
# This file invokes cmake and generates the build system for Clang.
#

if [ $# -lt 4 -o $# -gt 6 ]
then
  echo "Usage..."
  echo "gen-buildsys-clang.sh <path to top level CMakeLists.txt> <ClangMajorVersion> <ClangMinorVersion> <Architecture> [build flavor] [cmakeargs]"
  echo "Specify the path to the top level CMake file - <corefx>/src/Native/Unix"
  echo "Specify the clang version to use, split into major and minor version"
  echo "Specify the target architecture." 
  echo "Optionally specify the build configuration (flavor.) Defaults to DEBUG." 
  echo "Optionally pass additional arguments to CMake call."
  exit 1
fi

#Set the root directory of the project
project_root="$1"/../../..

# Set up the environment to be used for building with clang.
if which "clang-$2.$3" > /dev/null 2>&1
    then
        export CC="$(which clang-$2.$3)"
elif which "clang$2$3" > /dev/null 2>&1
    then
        export CC="$(which clang$2$3)"
elif which clang > /dev/null 2>&1
    then
        export CC="$(which clang)"
else
    echo "Unable to find Clang Compiler"
    exit 1
fi

build_arch="$4"
# Possible build types are DEBUG, RELEASE, RELWITHDEBINFO, MINSIZEREL.
# Default to DEBUG
if [ -z "$5" ]
then
  echo "Defaulting to DEBUG build."
  buildtype="DEBUG"
else
  buildtype="$5"
fi

cmake_extra_defines="-DCMAKE_BUILD_TYPE=$buildtype"
if [[ -n "$CROSSCOMPILE" ]]; then
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        echo "ROOTFS_DIR not set for crosscompile"
        exit 1
    fi
    if [[ -z "$CONFIG_DIR" ]]; then
        CONFIG_DIR="$project_root/cross"
    fi
    export TARGET_BUILD_ARCH=$build_arch
    cmake_extra_defines="$cmake_extra_defines -C $CONFIG_DIR/tryrun.cmake"
    cmake_extra_defines="$cmake_extra_defines -DCMAKE_TOOLCHAIN_FILE=$CONFIG_DIR/toolchain.cmake"
fi
if [ "$build_arch" == "armel" ]; then
    cmake_extra_defines="$cmake_extra_defines -DARM_SOFTFP=1"
fi

__UnprocessedCMakeArgs=""
if [ -z "$6" ]; then
    echo "No CMake extra Args specified"
else
    __UnprocessedCMakeArgs="$6"
fi

cmake $cmake_extra_defines \
    $__UnprocessedCMakeArgs \
    $1
