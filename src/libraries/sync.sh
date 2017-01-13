#!/usr/bin/env bash

if [ $# == 0 ]; then
    __args=-p
fi

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

$working_tree_root/init-tools.sh
if [ $? -ne 0 ]; then
    exit 1
fi

# Building CoreFx.Tools before calling build-managed.sh to workaround an Assembly loading bug caused by the new cli host.
"$working_tree_root/Tools/msbuild.sh" "$working_tree_root/src/Tools/CoreFx.Tools/CoreFx.Tools.csproj /v:m /m"
if [ $? -ne 0 ];then
   exit 1
fi

$working_tree_root/run.sh sync $__args $*
exit $?
