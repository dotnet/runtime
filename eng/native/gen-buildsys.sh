#!/usr/bin/env bash
#
# This file invokes cmake and generates the build system for Clang.
#

scriptroot="$( cd -P "$( dirname "$0" )" && pwd )"

if [[ "$#" -lt 4 ]]; then
  echo "Usage..."
  echo "gen-buildsys.sh <path to top level CMakeLists.txt> <path to tryrun.cmake directory> <path to intermediate directory> <Architecture> <compiler> <compiler major version> <compiler minor version> [build flavor] [ninja] [scan-build] [cmakeargs]"
  echo "Specify the path to the top level CMake file."
  echo "Specify the path that the build system files are generated in."
  echo "Specify the path to the directory with tryrun.cmake file."
  echo "Specify the target architecture."
  echo "Specify the name of compiler (clang or gcc)."
  echo "Specify the major version of compiler."
  echo "Specify the minor version of compiler."
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

source "$scriptroot/init-compiler.sh" "$build_arch" "$compiler" "$majorVersion" "$minorVersion"

CCC_CC="$CC"
CCC_CXX="$CXX"

export CCC_CC CCC_CXX

buildtype=DEBUG
code_coverage=OFF
build_tests=OFF
scan_build=OFF
generator="Unix Makefiles"
__UnprocessedCMakeArgs=""

for i in "${@:8}"; do
    upperI="$(echo "$i" | awk '{print toupper($0)}')"
    case "$upperI" in
      # Possible build types are DEBUG, CHECKED, RELEASE, RELWITHDEBINFO.
      DEBUG | CHECKED | RELEASE | RELWITHDEBINFO)
      buildtype="$upperI"
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

cmake_extra_defines=
if [[ "$CROSSCOMPILE" == "1" ]]; then
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        echo "ROOTFS_DIR not set for crosscompile"
        exit 1
    fi

    TARGET_BUILD_ARCH="$build_arch"
    export TARGET_BUILD_ARCH

    if [[ -n "$tryrun_dir" ]]; then
        cmake_extra_defines="$cmake_extra_defines -C $tryrun_dir/tryrun.cmake"
    fi
    cmake_extra_defines="$cmake_extra_defines -DCMAKE_TOOLCHAIN_FILE=$scriptroot/../common/cross/toolchain.cmake"
fi

if [[ "$build_arch" == "armel" ]]; then
    cmake_extra_defines="$cmake_extra_defines -DARM_SOFTFP=1"
fi

cmake_command=$(command -v cmake)

if [[ "$scan_build" == "ON" && -n "$SCAN_BUILD_COMMAND" ]]; then
    cmake_command="$SCAN_BUILD_COMMAND $cmake_command"
fi

if [[ "$build_arch" == "wasm" ]]; then
    cmake_command="emcmake $cmake_command"
fi

# We have to be able to build with CMake 3.6.2, so we can't use the -S or -B options
pushd "$3"

# Include CMAKE_USER_MAKE_RULES_OVERRIDE as uninitialized since it will hold its value in the CMake cache otherwise can cause issues when branch switching
$cmake_command \
  -G "$generator" \
  "-DCMAKE_BUILD_TYPE=$buildtype" \
  "-DCMAKE_INSTALL_PREFIX=$__CMakeBinDir" \
  $cmake_extra_defines \
  $__UnprocessedCMakeArgs \
  "$1"

popd
