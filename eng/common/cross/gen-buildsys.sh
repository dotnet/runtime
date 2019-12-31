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
  echo "gen-buildsys.sh <path to top level CMakeLists.txt> <path to intermediate directory> <path to tryrun.cmake directory> <Architecture> <compiler> <compiler major version> <compiler minor version> [build flavor] [ninja] [scan-build] [cmakeargs]"
  echo "Specify the path to the top level CMake file."
  echo "Specify the path to the directory with tryrun.cmake file."
  echo "Specify the path that the build system files are generated in."
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
cxxCompiler="$compiler++"
majorVersion="$6"
minorVersion="$7"

if [ "$compiler" = "gcc" ]; then cxxCompiler="g++"; fi

check_version_exists() {
    desired_version=-1

    # Set up the environment to be used for building with the desired compiler.
    if command -v "$compiler-$1.$2" > /dev/null; then
        desired_version="-$1.$2"
    elif command -v "$compier$1$2" > /dev/null; then
        desired_version="$1$2"
    elif command -v "$compiler-$1$2" > /dev/null; then
        desired_version="-$1$2"
    fi

    echo "$desired_version"
}

if [ -z "$CLR_CC" ]; then

    # Set default versions
    if [ -z "$majorVersion" ]; then
        # note: gcc (all versions) and clang versions higher than 6 do not have minor version in file name, if it is zero.
        if [ "$compiler" = "clang" ]; then versions=( 9 8 7 6.0 5.0 4.0 3.9 3.8 3.7 3.6 3.5 )
        elif [ "$compiler" = "gcc" ]; then versions=( 9 8 7 6 5 4.9 ); fi

        for version in "${versions[@]}"; do
            parts=(${version//./ })
            desired_version="$(check_version_exists "${parts[0]}" "${parts[1]}")"
            if [ "$desired_version" != "-1" ]; then majorVersion="${parts[0]}"; break; fi
        done

        if [ -z "$majorVersion" ]; then
            if command -v "$compiler" > /dev/null; then
                if [ "$(uname)" != "Darwin" ]; then
                    echo "WARN: Specific version of $compiler not found, falling back to use the one in PATH."
                fi
                export CC="$(command -v "$compiler")"
                export CXX="$(command -v "$cxxCompiler")"
            else
                echo "ERROR: No usable version of $compiler found."
                exit 1
            fi
        else
            if [ "$compiler" = "clang" ] && [ "$majorVersion" -lt 5 ]; then
                if [ "$build_arch" = "arm" ] || [ "$build_arch" = "armel" ]; then
                    if command -v "$compiler" > /dev/null; then
                        echo "WARN: Found clang version $majorVersion which is not supported on arm/armel architectures, falling back to use clang from PATH."
                        export CC="$(command -v "$compiler")"
                        export CXX="$(command -v "$cxxCompiler")"
                    else
                        echo "ERROR: Found clang version $majorVersion which is not supported on arm/armel architectures, and there is no clang in PATH."
                        exit 1
                    fi
                fi
            fi
        fi
    else
        desired_version="$(check_version_exists "$majorVersion" "$minorVersion")"
        if [ "$desired_version" = "-1" ]; then
            echo "ERROR: Could not find specific version of $compiler: $majorVersion $minorVersion."
            exit 1
        fi
    fi

    if [ -z "$CC" ]; then
        export CC="$(command -v "$compiler$desired_version")"
        export CXX="$(command -v "$cxxCompiler$desired_version")"
        if [ -z "$CXX" ]; then export CXX="$(command -v "$cxxCompiler")"; fi
    fi
else
    if [ ! -f "$CLR_CC" ]; then
        echo "ERROR: CLR_CC is set but path '$CLR_CC' does not exist"
        exit 1
    fi
    export CC="$CLR_CC"
    export CXX="$CLR_CXX"
fi

if [ -z "$CC" ]; then
    echo "ERROR: Unable to find $compiler."
    exit 1
fi

export CCC_CC="$CC"
export CCC_CXX="$CXX"
export SCAN_BUILD_COMMAND="$(command -v "scan-build$desired_version")"

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
