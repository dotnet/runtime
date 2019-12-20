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

if [ $# -lt 4 ]
then
  echo "Usage..."
  echo "gen-buildsys.sh <path to top level CMakeLists.txt> <path to intermediate directory> <Architecture> [build flavor] [ninja] [scan-build] [cmakeargs]"
  echo "Specify the path to the top level CMake file."
  echo "Specify the path that the build system files are generated in."
  echo "Specify the target architecture."
  echo "Optionally specify the build configuration (flavor.) Defaults to DEBUG."
  echo "Optionally specify 'scan-build' to enable build with clang static analyzer."
  echo "Use the Ninja generator instead of the Unix Makefiles generator."
  echo "Pass additional arguments to CMake call."
  exit 1
fi

tryrun_dir="$2"
build_arch="$4"
compiler="$5"
majorVersion="$6"
minorVersion="$7"

# Set up the environment to be used for building with the desired compiler.
if command -v "$compiler-$majorVersion.$minorVersion" > /dev/null; then
    desired_version="-$majorVersion.$minorVersion"
elif command -v "$compiler$majorVersion$minorVersion" > /dev/null; then
    desired_version="$majorVersion$minorVersion"
elif command -v "$compiler-$majorVersion$minorVersion" > /dev/null; then
    desired_version="-$majorVersion$minorVersion"
elif command -v "$compiler" > /dev/null; then
    desired_version=
fi

if [ -z "$CLR_CC" ]; then
    export CC="$(command -v "$compiler$desired_version")"
else
    export CC="$CLR_CC"
fi

if [ -z "$CLR_CXX" ]; then
    export CXX="$(command -v "$compiler++$desired_version")"
else
    export CXX="$CLR_CXX"
fi

if [ -z "$CC" ]; then
    echo "Unable to find $compiler"
    exit 1
fi

export CCC_CC="$CC"
export CCC_CXX="$CXX"
export SCAN_BUILD_COMMAND=$(command -v "scan-build$desired_version")

buildtype=DEBUG
code_coverage=OFF
build_tests=OFF
scan_build=OFF
generator="Unix Makefiles"
__UnprocessedCMakeArgs=""

for i in "${@:8}"; do
    upperI="$(echo $i | awk '{print toupper($0)}')"
    case $upperI in
      # Possible build types are DEBUG, CHECKED, RELEASE, RELWITHDEBINFO.
      DEBUG | CHECKED | RELEASE | RELWITHDEBINFO)
      buildtype=$upperI
      ;;
      NINJA)
      generator=Ninja
      ;;
      SCAN-BUILD)
      echo "Static analysis is turned on for this build."
      scan_build=ON
      ;;
      *)
      __UnprocessedCMakeArgs="${__UnprocessedCMakeArgs}${__UnprocessedCMakeArgs:+ }$i"
    esac
done

OS=`uname`

cmake_extra_defines=
if [ "$CROSSCOMPILE" == "1" ]; then
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        echo "ROOTFS_DIR not set for crosscompile"
        exit 1
    fi
    export TARGET_BUILD_ARCH=$build_arch
    cmake_extra_defines="$cmake_extra_defines -C $tryrun_dir/tryrun.cmake"
    cmake_extra_defines="$cmake_extra_defines -DCMAKE_TOOLCHAIN_FILE=$scriptroot/toolchain.cmake"
fi

cmake_command=$(command -v cmake)

if [[ "$scan_build" == "ON" && "$SCAN_BUILD_COMMAND" != "" ]]; then
    cmake_command="$SCAN_BUILD_COMMAND $cmake_command"
fi

# Include CMAKE_USER_MAKE_RULES_OVERRIDE as uninitialized since it will hold its value in the CMake cache otherwise can cause issues when branch switching
$cmake_command \
  -G "$generator" \
  "-DCMAKE_BUILD_TYPE=$buildtype" \
  "-DCMAKE_INSTALL_PREFIX=$__CMakeBinDir" \
  $cmake_extra_defines \
  $__UnprocessedCMakeArgs \
  -S "$1" \
  -B "$3"
