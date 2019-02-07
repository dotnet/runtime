#!/usr/bin/env sh
#
# This file invokes cmake and generates the build system for Gcc.
#

if [ $# -lt 4 ]
then
  echo "Usage..."
  echo "gen-buildsys-gcc.sh <path to top level CMakeLists.txt> <GccMajorVersion> <GccMinorVersion> <Architecture> [build flavor] [coverage] [ninja] [cmakeargs]"
  echo "Specify the path to the top level CMake file - <ProjectK>/src/NDP"
  echo "Specify the Gcc version to use, split into major and minor version"
  echo "Specify the target architecture." 
  echo "Optionally specify the build configuration (flavor.) Defaults to DEBUG." 
  echo "Optionally specify 'coverage' to enable code coverage build."
  echo "Target ninja instead of make. ninja must be on the PATH."
  echo "Pass additional arguments to CMake call."
  exit 1
fi

# Locate gcc
gcc_prefix=""

if [ "$CROSSCOMPILE" = "1" ]; then
  # Locate gcc
  if [ ! -z "$TOOLCHAIN" ]; then
    gcc_prefix="$TOOLCHAIN-"
  fi
fi

# Set up the environment to be used for building with gcc.
if command -v "${gcc_prefix}gcc-$2.$3" > /dev/null
    then
        desired_gcc_version="-$2.$3"
elif command -v "${gcc_prefix}gcc$2$3" > /dev/null
    then
        desired_gcc_version="$2$3"
elif command -v "${gcc_prefix}gcc-$2$3" > /dev/null
    then
        desired_gcc_version="-$2$3"
elif command -v ${gcc_prefix}gcc > /dev/null
    then
        desired_gcc_version=
else
    echo "Unable to find $gcc_prefixgcc Compiler"
    exit 1
fi

if [ -z "$CC" ]; then
  export CC="$(command -v ${gcc_prefix}gcc$desired_gcc_version)"
fi
if [ -z "$CXX" ]; then
  export CXX="$(command -v ${gcc_prefix}g++$desired_gcc_version)"
fi

build_arch="$4"
buildtype=DEBUG
code_coverage=OFF
generator="Unix Makefiles"
__UnprocessedCMakeArgs=""

ITER=-1
for i in "$@"; do
    ITER=$(($ITER + 1))
    if [ $ITER -lt 5 ]; then continue; fi
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

locate_gcc_exec() {
  if command -v "$gcc_prefix$1$desired_gcc_version" > /dev/null 2>&1
  then
    echo "$(command -v $gcc_prefix$1$desired_gcc_version)"
  elif command -v "$gcc_prefix$1" > /dev/null 2>&1
  then
    echo "$(command -v $gcc_prefix$1)"
  else
    exit 1
  fi
}

gcc_ar="$(locate_gcc_exec ar)"
[ $? -eq 0 ] || { echo "Unable to locate gcc-ar"; exit 1; }

if [ -z "$CC" ]; then
  gcc_link="$(locate_gcc_exec gcc)"
  [ $? -eq 0 ] || { echo "Unable to locate gcc-link"; exit 1; }
else
  gcc_link="$CC"
fi

if [ -z "$NM" ]; then
  gcc_nm="$(locate_gcc_exec nm)"
  [ $? -eq 0 ] || { echo "Unable to locate gcc-nm"; exit 1; }
else
  gcc_nm="$NM"
fi

if [ "$OS" = "Linux" ] || [ "$OS" = "FreeBSD" ] || [ "$OS" = "OpenBSD" ] || [ "$OS" = "NetBSD" ] || [ "$OS" = "SunOS" ]; then
  if [ -z "$OBJDUMP" ]; then
    gcc_objdump="$(locate_gcc_exec objdump)"
    [ $? -eq 0 ] || { echo "Unable to locate gcc-objdump"; exit 1; }
  else
    gcc_objdump="$OBJDUMP"
  fi
fi

if [ -z "$OBJCOPY" ]; then
  gcc_objcopy="$(locate_gcc_exec objcopy)"
  [ $? -eq 0 ] || { echo "Unable to locate gcc-objcopy"; exit 1; }
else
  gcc_objcopy="$OBJCOPY"
fi

gcc_ranlib="$(locate_gcc_exec ranlib)"
[ $? -eq 0 ] || { echo "Unable to locate gcc-ranlib"; exit 1; }

cmake_extra_defines=
if [ -n "$LLDB_LIB_DIR" ]; then
    cmake_extra_defines="$cmake_extra_defines -DWITH_LLDB_LIBS=$LLDB_LIB_DIR"
fi
if [ -n "$LLDB_INCLUDE_DIR" ]; then
    cmake_extra_defines="$cmake_extra_defines -DWITH_LLDB_INCLUDES=$LLDB_INCLUDE_DIR"
fi
if [ "$CROSSCOMPILE" = "1" ]; then
    if [ -z "$ROOTFS_DIR" ]; then
        echo "ROOTFS_DIR not set for crosscompile"
        exit 1
    fi
    if [ -z "$CONFIG_DIR" ]; then
        CONFIG_DIR="$1/cross"
    fi
    export TARGET_BUILD_ARCH=$build_arch
    cmake_extra_defines="$cmake_extra_defines -C $CONFIG_DIR/tryrun.cmake"
    cmake_extra_defines="$cmake_extra_defines -DCMAKE_TOOLCHAIN_FILE=$CONFIG_DIR/toolchain.cmake"
    cmake_extra_defines="$cmake_extra_defines --sysroot=$ROOTFS_DIR"
    cmake_extra_defines="$cmake_extra_defines -DCLR_UNIX_CROSS_BUILD=1"
fi
if [ "$OS" = "Linux" ]; then
    linux_id_file="/etc/os-release"
    if [ -n "$CROSSCOMPILE" ]; then
        linux_id_file="$ROOTFS_DIR/$linux_id_file"
    fi
    if [ -e "$linux_id_file" ]; then
        . "$linux_id_file"
        cmake_extra_defines="$cmake_extra_defines -DCLR_CMAKE_LINUX_ID=$ID"
    fi
fi
if [ "$build_arch" = "armel" ]; then
    cmake_extra_defines="$cmake_extra_defines -DARM_SOFTFP=1"
fi

overridefile=gcc-compiler-override.txt

# Determine the current script directory
__currentScriptDir="$(cd -- "$(dirname -- "$0")" && pwd -P)"

cmake \
  -G "$generator" \
  "-DCMAKE_USER_MAKE_RULES_OVERRIDE=${__currentScriptDir}/$overridefile" \
  "-DCMAKE_AR=$gcc_ar" \
  "-DCMAKE_LINKER=$gcc_link" \
  "-DCMAKE_NM=$gcc_nm" \
  "-DCMAKE_RANLIB=$gcc_ranlib" \
  "-DCMAKE_OBJCOPY=$gcc_objcopy" \
  "-DCMAKE_OBJDUMP=$gcc_objdump" \
  "-DCMAKE_BUILD_TYPE=$buildtype" \
  "-DCMAKE_EXPORT_COMPILE_COMMANDS=1 " \
  "-DCLR_CMAKE_ENABLE_CODE_COVERAGE=$code_coverage" \
  "-DCLR_CMAKE_COMPILER=GNU" \
  $cmake_extra_defines \
  $__UnprocessedCMakeArgs \
  "$1"
