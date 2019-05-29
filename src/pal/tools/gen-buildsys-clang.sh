#!/usr/bin/env bash
#
# This file invokes cmake and generates the build system for Clang.
#

if [ $# -lt 5 ]
then
  echo "Usage..."
  echo "gen-buildsys-clang.sh <path to top level CMakeLists.txt> <ClangMajorVersion> <ClangMinorVersion> <Architecture> <ScriptDirectory> [build flavor] [coverage] [ninja] [scan-build] [cmakeargs]"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Specify the clang version to use, split into major and minor version"
  echo "Specify the target architecture."
  echo "Specify the script directory."
  echo "Optionally specify the build configuration (flavor.) Defaults to DEBUG." 
  echo "Optionally specify 'coverage' to enable code coverage build."
  echo "Optionally specify 'scan-build' to enable build with clang static analyzer."
  echo "Target ninja instead of make. ninja must be on the PATH."
  echo "Pass additional arguments to CMake call."
  exit 1
fi

# Set up the environment to be used for building with clang.
if command -v "clang-$2.$3" > /dev/null
    then
        desired_llvm_version="-$2.$3"
elif command -v "clang$2$3" > /dev/null
    then
        desired_llvm_version="$2$3"
elif command -v "clang-$2$3" > /dev/null
    then
        desired_llvm_version="-$2$3"
elif command -v clang > /dev/null
    then
        desired_llvm_version=
else
    echo "Unable to find Clang Compiler"
    exit 1
fi

export CC="$(command -v clang$desired_llvm_version)"
export CXX="$(command -v clang++$desired_llvm_version)"

build_arch="$4"
script_dir="$5"
buildtype=DEBUG
code_coverage=OFF
build_tests=OFF
scan_build=OFF
generator="Unix Makefiles"
__UnprocessedCMakeArgs=""

for i in "${@:6}"; do
    upperI="$(echo $i | awk '{print toupper($0)}')"
    case $upperI in
      # Possible build types are DEBUG, CHECKED, RELEASE, RELWITHDEBINFO, MINSIZEREL.
      DEBUG | CHECKED | RELEASE | RELWITHDEBINFO | MINSIZEREL)
      buildtype=$upperI
      ;;
      COVERAGE)
      echo "Code coverage is turned on for this build."
      code_coverage=ON
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

# Locate llvm
# This can be a little complicated, because the common use-case of Ubuntu with
# llvm-3.5 installed uses a rather unusual llvm installation with the version
# number postfixed (i.e. llvm-ar-3.5), so we check for that first.
# On FreeBSD the version number is appended without point and dash (i.e.
# llvm-ar35).
# Additionally, OSX doesn't use the llvm- prefix.
if [ $OS = "Linux" -o $OS = "FreeBSD" -o $OS = "OpenBSD" -o $OS = "NetBSD" -o $OS = "SunOS" ]; then
  llvm_prefix="llvm-"
elif [ $OS = "Darwin" ]; then
  llvm_prefix=""
else
  echo "Unable to determine build platform"
  exit 1
fi

locate_llvm_exec() {
  if command -v "$llvm_prefix$1$desired_llvm_version" > /dev/null 2>&1
  then
    echo "$(command -v $llvm_prefix$1$desired_llvm_version)"
  elif command -v "$llvm_prefix$1" > /dev/null 2>&1
  then
    echo "$(command -v $llvm_prefix$1)"
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
if [ $OS = "Linux" -o $OS = "FreeBSD" -o $OS = "OpenBSD" -o $OS = "NetBSD" -o $OS = "SunOS" ]; then
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
if [ "$CROSSCOMPILE" == "1" ]; then
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        echo "ROOTFS_DIR not set for crosscompile"
        exit 1
    fi
    if [[ -z $CONFIG_DIR ]]; then
        CONFIG_DIR="$1/cross"
    fi
    export TARGET_BUILD_ARCH=$build_arch
    cmake_extra_defines="$cmake_extra_defines -C $CONFIG_DIR/tryrun.cmake"
    cmake_extra_defines="$cmake_extra_defines -DCMAKE_TOOLCHAIN_FILE=$CONFIG_DIR/toolchain.cmake"
    cmake_extra_defines="$cmake_extra_defines -DCLR_UNIX_CROSS_BUILD=1"
fi
if [ $OS == "Linux" ]; then
    linux_id_file="/etc/os-release"
    if [[ -n "$CROSSCOMPILE" ]]; then
        linux_id_file="$ROOTFS_DIR/$linux_id_file"
    fi
    if [[ -e $linux_id_file ]]; then
        source $linux_id_file
        cmake_extra_defines="$cmake_extra_defines -DCLR_CMAKE_LINUX_ID=$ID"
    fi
fi
if [ "$build_arch" == "armel" ]; then
    cmake_extra_defines="$cmake_extra_defines -DARM_SOFTFP=1"
fi

__currentScriptDir="$script_dir"

cmake_command=$(command -v cmake3 || command -v cmake)

if [[ "$scan_build" == "ON" ]]; then
    export CCC_CC=$CC
    export CCC_CXX=$CXX
    export SCAN_BUILD_COMMAND=$(command -v scan-build$desired_llvm_version)
    cmake_command="$SCAN_BUILD_COMMAND $cmake_command"
fi

# Include CMAKE_USER_MAKE_RULES_OVERRIDE as uninitialized since it will hold its value in the CMake cache otherwise can cause issues when branch switching
$cmake_command \
  -G "$generator" \
  "-DCMAKE_AR=$llvm_ar" \
  "-DCMAKE_LINKER=$llvm_link" \
  "-DCMAKE_NM=$llvm_nm" \
  "-DCMAKE_OBJDUMP=$llvm_objdump" \
  "-DCMAKE_BUILD_TYPE=$buildtype" \
  "-DCLR_CMAKE_ENABLE_CODE_COVERAGE=$code_coverage" \
  "-DCMAKE_INSTALL_PREFIX=$__CMakeBinDir" \
  "-DCLR_CMAKE_COMPILER=Clang" \
  "-DCMAKE_USER_MAKE_RULES_OVERRIDE=" \
  $cmake_extra_defines \
  $__UnprocessedCMakeArgs \
  "$1"
