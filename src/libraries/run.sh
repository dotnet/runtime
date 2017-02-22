#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
$__scriptpath/init-tools.sh
if [ $? -ne 0 ]; then
    exit 1
fi

# Always copy over the Tools-Override
cp $__scriptpath/Tools-Override/* $__scriptpath/Tools > /dev/null

__toolRuntime=$__scriptpath/Tools
__dotnet=$__toolRuntime/dotnetcli/dotnet

$__dotnet $__toolRuntime/run.exe $*
exit $?
