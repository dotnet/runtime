#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [useservergc]"
    echo
    echo "useservergc - Switch used by configure the hosted coreclr instance when executing tests."
    echo
}

__UnprocessedBuildArgs=
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

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
$working_tree_root/run.sh build-managed -testsOnly $__UnprocessedBuildArgs
exit $?
