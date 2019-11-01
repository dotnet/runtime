#!/usr/bin/env sh
#
# This file finds GCC on the system.
#

if [ $# -lt 2 ]
then
  echo "Usage..."
  echo "find-gcc.sh <GccMajorVersion> <GccMinorVersion>"
  echo "Specify the Gcc version to use, split into major and minor version"
  exit 1
fi

# Locate gcc
gcc_prefix=""

if [ "$CROSSCOMPILE" = "1" ]; then
  # Locate gcc
  if [ -n "$TOOLCHAIN" ]; then
    gcc_prefix="$TOOLCHAIN-"
  fi
fi

# Set up the environment to be used for building with gcc.
if command -v "${gcc_prefix}gcc-$1.$2" > /dev/null
    then
        desired_gcc_version="-$1.$2"
elif command -v "${gcc_prefix}gcc$1$2" > /dev/null
    then
        desired_gcc_version="$1$2"
elif command -v "${gcc_prefix}gcc-$1$2" > /dev/null
    then
        desired_gcc_version="-$1$2"
elif command -v "${gcc_prefix}gcc" > /dev/null
    then
        desired_gcc_version=
else
    echo "Unable to find ${gcc_prefix}gcc Compiler"
    exit 1
fi

if [ -z "$CLR_CC" ]; then
    CC="$(command -v "${gcc_prefix}gcc$desired_gcc_version")"
else
    CC="$CLR_CC"
fi

if [ -z "$CLR_CXX" ]; then
    CXX="$(command -v "${gcc_prefix}g++$desired_gcc_version")"
else
    CXX="$CLR_CXX"
fi

export CC
export CXX
