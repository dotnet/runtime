#!/bin/bash

# Arguments:
#-------------------------------------------------------
# $1 Visual Studio target, build|clean, default build
# $2 Host CPU architecture, x86_64|i686, default x86_64
# $3 Visual Studio configuration, debug|release, default release
# $4 Additional arguments passed to msbuild, needs to be quoted if multiple.
#-------------------------------------------------------

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

RUN_MSBUILD_SCRIPT_PATH=$(cd "$(dirname "$0")"; pwd)
RUN_MSBUILD_SCRIPT_PATH=$(win32_format_path "$RUN_MSBUILD_SCRIPT_PATH/run-msbuild.bat")

WINDOWS_CMD=$(which cmd.exe)
if [ ! -f $WINDOWS_CMD ]; then
    WINDOWS_CMD=$WINDIR/System32/cmd.exe
fi

"$WINDOWS_CMD" /c "$RUN_MSBUILD_SCRIPT_PATH" "$@"
