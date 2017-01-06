#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [platform] [useservergc]"
    echo
    echo "platform can be: FreeBSD, Linux, NetBSD, OSX, Windows."
    echo
}


__scriptpath=$(cd "$(dirname "$0")"; pwd -P)

__UnprocessedBuildArgs=
__TestNugetRuntimeId=-distroRid
__BuildOS=-os
__TargetOS=-target-os

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        freebsd)
            __TestNugetRuntimeId="$__TestNugetRuntimeId=osx.10.10-x64"
            __BuildOS="$__BuildOS=freebsd"
            __TargetOS="$__TargetOS=freebsd"
            ;;
        netbsd)
            __TestNugetRuntimeId="$__TestNugetRuntimeId=osx.10.10-x64"
            __BuildOS="$__BuildOS=netbsd"
            __TargetOS="$__TargetOS=netbsd"
            ;;
        osx)
            __TestNugetRuntimeId="$__TestNugetRuntimeId=osx.10.10-x64"
            __BuildOS="$__BuildOS=osx"
            __TargetOS="$__TargetOS=osx"
            ;;
        windows)
            __TestNugetRuntimeId="$__TestNugetRuntimeId=win7-x64"
            __BuildOS="$__BuildOS=windows_nt"
            __TargetOS="$__TargetOS=Windows_NT"
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
    esac

    shift
done

$__scriptpath/run.sh build-managed $__BuildOS $__TargetOS $__TestNugetRuntimeId $__UnprocessedBuildArgs
exit $?
