#!/usr/bin/env bash
#
# This file invokes cmake and generates the build system for Clang.
#

if [ $# -lt 4 ]
then
  echo "Usage..."
  echo "gen-buildsys-clang.sh <path to top level CMakeLists.txt> <ClangMajorVersion> <ClangMinorVersion> <Architecture> [build flavor] [coverage] [ninja] [cmakeargs]"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Specify the clang version to use, split into major and minor version"
  echo "Specify the target architecture." 
  echo "Optionally specify the build configuration (flavor.) Defaults to DEBUG." 
  echo "Optionally specify 'coverage' to enable code coverage build."
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
buildtype=DEBUG
code_coverage=OFF
build_tests=OFF
generator="Unix Makefiles"
__UnprocessedCMakeArgs=""

for i in "${@:5}"; do
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

clang_version=$( $CC --version | head -1 | sed 's/[^0-9]*\([0-9]*\.[0-9]*\).*/\1/' )
# Use O1 option when the clang version is smaller than 3.9
# Otherwise use O3 option in release build
if [[ ( ${clang_version%.*} -eq 3  &&  ${clang_version#*.} -lt 9 ) &&
      (  "$build_arch" == "arm" || "$build_arch" == "armel" ) ]]; then
    overridefile=clang-compiler-override-arm.txt
else
    overridefile=clang-compiler-override.txt
fi

# Determine the current script directory
__currentScriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

cmake \
  -G "$generator" \
  "-DCMAKE_USER_MAKE_RULES_OVERRIDE=${__currentScriptDir}/$overridefile" \
  "-DCMAKE_AR=$llvm_ar" \
  "-DCMAKE_LINKER=$llvm_link" \
  "-DCMAKE_NM=$llvm_nm" \
  "-DCMAKE_OBJDUMP=$llvm_objdump" \
  "-DCMAKE_BUILD_TYPE=$buildtype" \
  "-DCMAKE_EXPORT_COMPILE_COMMANDS=1 " \
  "-DCLR_CMAKE_ENABLE_CODE_COVERAGE=$code_coverage" \
  $cmake_extra_defines \
  $__UnprocessedCMakeArgs \
  "$1"
