#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)

# We do not want to run the first-time experience.
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism 
export DOTNET_MULTILEVEL_LOOKUP=0

# Source the init-tools.sh script rather than execute in order to preserve ulimit values in child-processes. https://github.com/dotnet/corefx/issues/19152
. $__scriptpath/init-tools.sh

__toolRuntime=$__scriptpath/Tools
__dotnet=$__toolRuntime/dotnetcli/dotnet

cd $__scriptpath
$__dotnet $__toolRuntime/run.exe $__scriptpath/config.json $*
exit $?
