#
# This file invokes cmake and generates the build system for gcc.
#
#!/bin/bash

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


cmake -DCMAKE_USER_MAKE_RULES_OVERRIDE=$1/src/pal/tools/clang-compiler-override.txt -D_CMAKE_TOOLCHAIN_PREFIX=llvm- -D_CMAKE_TOOLCHAIN_SUFFIX=-3.5 -DCMAKE_BUILD_TYPE=$buildtype $1

