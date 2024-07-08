#!/usr/bin/env bash
#
# This file invokes cmake and generates the build system for Clang.
#

scriptroot="$( cd -P "$( dirname "$0" )" && pwd )"

if [[ "$#" -lt 4 ]]; then
  echo "Usage..."
  echo "gen-buildsys.sh <path to top level CMakeLists.txt> <path to intermediate directory> <Architecture> <Os> <compiler> [build flavor] [ninja] [scan-build] [cmakeargs]"
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
target_os="$4"
compiler="$5"

if [[ "$compiler" != "default" ]]; then
    nativescriptroot="$( cd -P "$scriptroot/../common/native" && pwd )"
    build_arch="$host_arch" compiler="$compiler" . "$nativescriptroot/init-compiler.sh"

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

for i in "${@:6}"; do
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
    platform="$(uname -s | tr '[:upper:]' '[:lower:]')"
    # OSX doesn't use rootfs
    if ! [[ -n "$ROOTFS_DIR" || "$platform" == "darwin" ]]; then
        echo "ROOTFS_DIR not set for crosscompile"
        exit 1
    fi

    TARGET_BUILD_ARCH="$host_arch"
    export TARGET_BUILD_ARCH

    cmake_extra_defines="$cmake_extra_defines -C $scriptroot/tryrun.cmake"

    if [[ "$platform" == "darwin" ]]; then
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
    if [[ "$target_os" == "browser" ]]; then
        cmake_command="emcmake $cmake_command"
    elif [[ "$target_os" == "wasi" ]]; then
        true
    else
        echo "target_os was not specified"
        exit 1
    fi
fi

$cmake_command \
  --no-warn-unused-cli \
  -G "$generator" \
  "-DCMAKE_BUILD_TYPE=$buildtype" \
  "-DCMAKE_INSTALL_PREFIX=$__CMakeBinDir" \
  $cmake_extra_defines \
  $__UnprocessedCMakeArgs \
  -S "$1" \
  -B "$2"

# don't add anything after this line so the cmake exit code gets propagated correctly
