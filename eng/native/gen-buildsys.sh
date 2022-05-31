#!/usr/bin/env bash
#
# This file invokes cmake and generates the build system for Clang.
#

scriptroot="$( cd -P "$( dirname "$0" )" && pwd )"

if [[ "$#" -lt 4 ]]; then
  echo "Usage..."
  echo "gen-buildsys.sh <path to top level CMakeLists.txt> <path to intermediate directory> <Architecture> <compiler> [build flavor] [ninja] [scan-build] [cmakeargs]"
  echo "Specify the path to the top level CMake file."
  echo "Specify the path that the build system files are generated in."
  echo "Specify the host architecture (the architecture the built tools should run on)."
  echo "Specify the name of compiler (clang or gcc)."
  echo "Optionally specify the build configuration (flavor.) Defaults to DEBUG."
  echo "Optionally specify 'scan-build' to enable build with clang static analyzer."
  echo "Use the Ninja generator instead of the Unix Makefiles generator."
  echo "Pass additional arguments to CMake call."
  exit 1
fi

host_arch="$3"
compiler="$4"

if [[ "$compiler" != "default" ]]; then
    nativescriptroot="$( cd -P "$scriptroot/../common/native" && pwd )"
    source "$nativescriptroot/init-compiler.sh" "$nativescriptroot" "$host_arch" "$compiler"

    CCC_CC="$CC"
    CCC_CXX="$CXX"
fi

export CCC_CC CCC_CXX

buildtype=DEBUG
code_coverage=OFF
build_tests=OFF
scan_build=OFF
generator="Unix Makefiles"
__UnprocessedCMakeArgs=""

for i in "${@:5}"; do
    upperI="$(echo "$i" | tr "[:lower:]" "[:upper:]")"
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
    platform="$(uname)"
    # OSX doesn't use rootfs
    if ! [[ -n "$ROOTFS_DIR" || "$platform" == "Darwin" ]]; then
        echo "ROOTFS_DIR not set for crosscompile"
        exit 1
    fi

    TARGET_BUILD_ARCH="$host_arch"
    export TARGET_BUILD_ARCH

    cmake_extra_defines="$cmake_extra_defines -C $scriptroot/tryrun.cmake"

    if [[ "$platform" == "Darwin" ]]; then
        cmake_extra_defines="$cmake_extra_defines -DCMAKE_SYSTEM_NAME=Darwin"
    else
        cmake_extra_defines="$cmake_extra_defines -DCMAKE_TOOLCHAIN_FILE=$scriptroot/../common/cross/toolchain.cmake"
    fi
fi

if [[ "$host_arch" == "armel" ]]; then
    cmake_extra_defines="$cmake_extra_defines -DARM_SOFTFP=1"
fi

if ! cmake_command=$(command -v cmake); then
    echo "CMake was not found in PATH."
    exit 1
fi

if [[ "$scan_build" == "ON" && -n "$SCAN_BUILD_COMMAND" ]]; then
    cmake_command="$SCAN_BUILD_COMMAND $cmake_command"
fi

if [[ "$host_arch" == "wasm" ]]; then
    cmake_command="emcmake $cmake_command"
fi

# We have to be able to build with CMake 3.6.2, so we can't use the -S or -B options
pushd "$2"

$cmake_command \
  --no-warn-unused-cli \
  -G "$generator" \
  "-DCMAKE_BUILD_TYPE=$buildtype" \
  "-DCMAKE_INSTALL_PREFIX=$__CMakeBinDir" \
  $cmake_extra_defines \
  $__UnprocessedCMakeArgs \
  "$1"

popd
