#!/usr/bin/env bash
#
# This file finds Clang on the system.
#

if [ $# -lt 2 ]
then
  echo "Usage..."
  echo "find-clang.sh <ClangMajorVersion> <ClangMinorVersion>"
  echo "Specify the clang version to use, split into major and minor version"
  exit 1
fi

# Set up the environment to be used for building with clang.
if command -v "clang-$1.$2" > /dev/null
    then
        desired_llvm_version="-$1.$2"
elif command -v "clang$1$2" > /dev/null
    then
        desired_llvm_version="$1$2"
elif command -v "clang-$1$2" > /dev/null
    then
        desired_llvm_version="-$1$2"
elif command -v clang > /dev/null
    then
        desired_llvm_version=
else
    echo "Unable to find Clang Compiler"
    exit 1
fi

export CC="$(command -v clang$desired_llvm_version)"
export CXX="$(command -v clang++$desired_llvm_version)"
export CCC_CC=$CC
export CCC_CXX=$CXX
export SCAN_BUILD_COMMAND=$(command -v scan-build$desired_llvm_version)
