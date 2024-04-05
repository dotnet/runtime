#!/bin/sh
#
# This file detects the C/C++ compiler and exports it to the CC/CXX environment variables
#
# NOTE: some scripts source this file and rely on stdout being empty, make sure to not output anything here!

if [ -z "$build_arch" ] || [ -z "$compiler" ]; then
  echo "Usage..."
  echo "build_arch=<ARCH> compiler=<NAME> init-compiler.sh"
  echo "Specify the target architecture."
  echo "Specify the name of compiler (clang or gcc)."
  exit 1
fi

case "$compiler" in
    clang*|-clang*|--clang*)
        # clangx.y or clang-x.y
        version="$(echo "$compiler" | tr -d '[:alpha:]-=')"
        majorVersion="${version%%.*}"
        [ -z "${version##*.*}" ] && minorVersion="${version#*.}"

        if [ -z "$minorVersion" ] && [ -n "$majorVersion" ] && [ "$majorVersion" -le 6 ]; then
            minorVersion=0;
        fi
        compiler=clang
        ;;

    gcc*|-gcc*|--gcc*)
        # gccx.y or gcc-x.y
        version="$(echo "$compiler" | tr -d '[:alpha:]-=')"
        majorVersion="${version%%.*}"
        [ -z "${version##*.*}" ] && minorVersion="${version#*.}"
        compiler=gcc
        ;;
esac

cxxCompiler="$compiler++"

# clear the existing CC and CXX from environment
CC=
CXX=
LDFLAGS=

if [ "$compiler" = "gcc" ]; then cxxCompiler="g++"; fi

check_version_exists() {
    desired_version=-1

    # Set up the environment to be used for building with the desired compiler.
    if command -v "$compiler-$1.$2" > /dev/null; then
        desired_version="-$1.$2"
    elif command -v "$compiler$1$2" > /dev/null; then
        desired_version="$1$2"
    elif command -v "$compiler-$1$2" > /dev/null; then
        desired_version="-$1$2"
    fi

    echo "$desired_version"
}

if [ -z "$CLR_CC" ]; then

    # Set default versions
    if [ -z "$majorVersion" ]; then
        if ! command -v "$compiler" > /dev/null; then
            echo "No usable version of $compiler found."
            exit 1
        fi

        CC="$(command -v "$compiler")"
        CXX="$(command -v "$cxxCompiler")"

        version="$("$CC" -dumpversion)"
        # gcc and clang often display 3 part versions. However, gcc can show only 1 part in some environments.
        IFS=. read -r majorVersion minorVersion patchVersion <<EOF
$version
EOF
        unset patchVersion

        if [ "$compiler" = "clang" ] && [ "$majorVersion" -lt 5 ]; then
            if [ "$build_arch" = "arm" ] || [ "$build_arch" = "armel" ]; then
                echo "Found clang version $majorVersion which is not supported on arm/armel architectures."
                exit 1
            fi
        fi

    else
        desired_version="$(check_version_exists "$majorVersion" "$minorVersion")"
        if [ "$desired_version" = "-1" ]; then
            echo "Could not find specific version of $compiler: $majorVersion $minorVersion."
            exit 1
        fi
        CC="$(command -v "$compiler$desired_version")"
        CXX="$(command -v "$cxxCompiler$desired_version")"
        if [ -z "$CXX" ]; then CXX="$(command -v "$cxxCompiler")"; fi
    fi
else
    if [ ! -f "$CLR_CC" ]; then
        echo "CLR_CC is set but path '$CLR_CC' does not exist"
        exit 1
    fi
    CC="$CLR_CC"
    CXX="$CLR_CXX"
fi

if [ -z "$CC" ]; then
    echo "Unable to find $compiler."
    exit 1
fi

# Only lld version >= 9 can be considered stable. lld supports s390x starting from 18.0.
if [ "$compiler" = "clang" ] && [ -n "$majorVersion" ] && [ "$majorVersion" -ge 9 ] && ([ "$build_arch" != "s390x" ] || [ "$majorVersion" -ge 18 ]); then
    if "$CC" -fuse-ld=lld -Wl,--version >/dev/null 2>&1; then
        LDFLAGS="-fuse-ld=lld"
    fi
fi

SCAN_BUILD_COMMAND="$(command -v "scan-build$desired_version")"

export CC CXX LDFLAGS SCAN_BUILD_COMMAND
