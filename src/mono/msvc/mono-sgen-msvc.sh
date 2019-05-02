#!/bin/bash

# Wrapper that can be used as MONO_EXECUTABLE_WRAPPER in mono-wrapper when running MSVC build
# mono-sgen.exe. Simplify the setup of VS and MSVC toolchain, primarly when running MSVC build
# mono-sgen.exe as AOT compiler, since it needs to locate libraries as well as ClangC2 and linker
# from VS MSVC for corresponding architecture.

# NOTE, MSVC build mono-sgen.exe AOT compiler currently support 64-bit AMD codegen. mono-sgen-msvc.bat will ony setup
# amd64 versions of VS MSVC build environment and corresponding ClangC2 compiler.

# Optimization, only run full build environment when running mono-sgen.exe as AOT compiler.
# If not, just run mono-sgen.exe with supplied arguments.

function win32_format_path {
    local formatted_path=$1
    local host_win32_wsl=0
    local host_win32_cygwin=0

    host_uname="$(uname -a)"
    case "$host_uname" in
        *Microsoft*)
            host_win32_wsl=1
            ;;
        CYGWIN*)
            host_win32_cygwin=1
            ;;
	esac

    if  [[ $host_win32_wsl = 1 ]] && [[ $1 == "/mnt/"* ]]; then
        formatted_path="$(wslpath -a -w "$1")"
    elif [[ $host_win32_cygwin = 1 ]] && [[ $1 == "/cygdrive/"* ]]; then
        formatted_path="$(cygpath -a -w "$1")"
    fi

    echo "$formatted_path"
}

MONO_SGEN_MSVC_SCRIPT_PATH=$(cd "$(dirname "$0")"; pwd)

if [[ "$@" != *"--aot="* ]]; then
    "$MONO_SGEN_MSVC_SCRIPT_PATH/mono-sgen.exe" "$@"
else
    MONO_SGEN_MSVC_SCRIPT_PATH=$(win32_format_path "$MONO_SGEN_MSVC_SCRIPT_PATH/mono-sgen-msvc.bat")

    WINDOWS_CMD=$(which cmd.exe)
    if [ ! -f $WINDOWS_CMD ]; then
        WINDOWS_CMD=$WINDIR/System32/cmd.exe
    fi
    "$WINDOWS_CMD" /c "$MONO_SGEN_MSVC_SCRIPT_PATH" "$@"
fi
