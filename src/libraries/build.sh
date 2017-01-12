#!/usr/bin/env bash

usage()
{
    echo
    echo "There are new changes on how we build. Use this script only for generic"
    echo "build instructions that apply for both build native and build managed."
    echo "Otherwise:"
    echo
    echo "Before                Now"
    echo "build.sh native      build-native.sh"
    echo "build.sh managed     build-managed.sh"
    echo
    echo "For more information: https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md"
    echo "----------------------------------------------------------------------------"
    echo
    echo
}

if [ "$1" == "-?" ]; then
    usage
fi

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)

"$__scriptpath/build-native.sh" $*
if [ $? -ne 0 ];then
   exit 1
fi

# Building CoreFx.Tools before calling build-managed.sh to workaround an Assembly loading bug caused by the new cli host.
"$__scriptpath/Tools/msbuild.sh" "$__scriptpath/src/Tools/CoreFx.Tools/CoreFx.Tools.csproj" /v:m /m
if [ $? -ne 0 ];then
   exit 1
fi
"$__scriptpath/build-managed.sh" $*
exit $?
