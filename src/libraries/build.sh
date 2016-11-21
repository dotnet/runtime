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

__ServerGC=0

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        useservergc)
            __ServerGC=1
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
    esac

    shift
done

export CORECLR_SERVER_GC="$__ServerGC"

"$__scriptpath/build-native.sh" $*
if [ $? -ne 0 ];then
   exit 1
fi

"$__scriptpath/build-packages.sh" -DisableManagedPackage $*
if [ $? -ne 0 ];then
   exit 1
fi

"$__scriptpath/build-managed.sh" $*
if [ $? -ne 0 ];then
   exit 1
fi

"$__scriptpath/build-tests.sh" $*
exit $?

