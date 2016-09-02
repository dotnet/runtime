#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
$__scriptpath/init-tools.sh
if [ $? -ne 0 ]; then
    exit 1
fi

__toolRuntime=$__scriptpath/Tools
__dotnet=$__toolRuntime/dotnetcli/dotnet

$__dotnet $__toolRuntime/run.exe $*
exit $?
