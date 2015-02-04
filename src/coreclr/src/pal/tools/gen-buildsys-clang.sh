#!/bin/bash
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

. $1/src/pal/tools/setup-compiler-clang.sh

# Possible build types are DEBUG, RELEASE, RELWITHDEBINFO, MINSIZEREL.
# Default to DEBUG
if [ -z "$2" ]
then
  echo "Defaulting to DEBUG build."
  buildtype="DEBUG"
else
  buildtype=$2
fi

# Locate llvm
# This can be a little complicated, because the common use-case of Ubuntu with
# llvm-3.5 installed uses a rather unusual llvm installation with the version
# number postfixed (i.e. llvm-ar-3.5), so we check for that first.
desired_llvm_version=3.5
locate_llvm_exec() {
  if which $1-$desired_llvm_version > /dev/null 2>&1
  then
    echo $(which $1-$desired_llvm_version)
  elif which $1 > /dev/null 2>&1
  then
    echo $(which $1)
  else
    exit 1
  fi
}
llvm_ar=$(locate_llvm_exec llvm-ar)
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-ar"; exit 1; }
llvm_link=$(locate_llvm_exec llvm-link)
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-link"; exit 1; }
llvm_nm=$(locate_llvm_exec llvm-nm)
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-nm"; exit 1; }
llvm_objdump=$(locate_llvm_exec llvm-objdump)
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-objdump"; exit 1; }
llvm_ranlib=$(locate_llvm_exec llvm-ranlib)
[[ $? -eq 0 ]] || { echo "Unable to locate llvm-ranlib"; exit 1; }

cmake -DCMAKE_USER_MAKE_RULES_OVERRIDE=$1/src/pal/tools/clang-compiler-override.txt \
  -DCMAKE_AR=$llvm_ar \
  -DCMAKE_LINKER=$llvm_link \
  -DCMAKE_NM=$llvm_nm \
  -DCMAKE_OBJDUMP=$llvm_objdump \
  -DCMAKE_RANLIB=$llvm_ranlib \
  -DCMAKE_BUILD_TYPE=$buildtype \
  $1

