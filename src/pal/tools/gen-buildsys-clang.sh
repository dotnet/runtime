#!/usr/bin/env bash
#
# This file invokes cmake and generates the build system for gcc.
#

if [ $# -lt 1 -o $# -gt 2 ]
then
  echo "Usage..."
  echo "gen-buildsys-clang.sh <path to top level CMakeLists.txt> [build flavor]"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Optionally specify the build configuration (flavor.) Defaults to DEBUG." 
  exit 1
fi

. "$1/src/pal/tools/setup-compiler-clang.sh"

# Possible build types are DEBUG, RELEASE, RELWITHDEBINFO, MINSIZEREL.
# Default to DEBUG
if [ -z "$2" ]
then
  echo "Defaulting to DEBUG build."
  buildtype="DEBUG"
else
  buildtype="$2"
fi

OS=`uname`

# Locate llvm
# This can be a little complicated, because the common use-case of Ubuntu with
# llvm-3.5 installed uses a rather unusual llvm installation with the version
# number postfixed (i.e. llvm-ar-3.5), so we check for that first.
# Additionally, OSX doesn't use the llvm- prefix.
if [ $OS = "Linux" ]; then
  llvm_prefix="llvm-"
elif [ $OS = "Darwin" ]; then
  llvm_prefix=""
else
  echo "Unable to determine build platform"
  exit 1
fi

desired_llvm_version=3.5
locate_llvm_exec() {
  if which "$llvm_prefix$1-$desired_llvm_version" > /dev/null 2>&1
  then
    echo "$(which $llvm_prefix$1-$desired_llvm_version)"
  elif which "$1" > /dev/null 2>&1
  then
    echo "$(which $1)"
  else
    exit 1
  fi
}
llvm_ar="$(locate_llvm_exec ar)"
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-ar"; exit 1; }
llvm_link="$(locate_llvm_exec link)"
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-link"; exit 1; }
llvm_nm="$(locate_llvm_exec nm)"
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-nm"; exit 1; }
llvm_ranlib="$(locate_llvm_exec ranlib)"
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-ranlib"; exit 1; }
if [ $OS = "Linux" ]; then
  llvm_objdump="$(locate_llvm_exec objdump)"
  [[ $? -eq 0 ]] || { echo "Unable to locate llvm-objdump"; exit 1; }
fi

cmake_extra_defines=
if [[ -n "$LLDB_LIB_DIR" ]]; then
    cmake_extra_defines="$cmake_extra_defines -DWITH_LLDB_LIBS=$LLDB_LIB_DIR"
fi
if [[ -n "$LLDB_INCLUDE_DIR" ]]; then
    cmake_extra_defines="$cmake_extra_defines -DWITH_LLDB_INCLUDES=$LLDB_INCLUDE_DIR"
fi

cmake \
  "-DCMAKE_USER_MAKE_RULES_OVERRIDE=$1/src/pal/tools/clang-compiler-override.txt" \
  "-DCMAKE_AR=$llvm_ar" \
  "-DCMAKE_LINKER=$llvm_link" \
  "-DCMAKE_NM=$llvm_nm" \
  "-DCMAKE_OBJDUMP=$llvm_objdump" \
  "-DCMAKE_RANLIB=$llvm_ranlib" \
  "-DCMAKE_BUILD_TYPE=$buildtype" \
  $cmake_extra_defines \
  "$1"
