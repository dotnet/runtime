#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)

# Source the init-tools.sh script rather than execute in order to preserve ulimit values in child-processes. https://github.com/dotnet/corefx/issues/19152
. $__scriptpath/init-tools.sh

__toolRuntime=$__scriptpath/Tools
__dotnet=$__toolRuntime/dotnetcli/dotnet

cd $__scriptpath
$__dotnet $__toolRuntime/run.exe $__scriptpath/config.json $*
exit $?
