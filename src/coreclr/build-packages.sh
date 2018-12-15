#!/usr/bin/env bash

usage()
{
    echo "Builds the NuGet packages from the binaries that were built in the Build product binaries step."
    echo "Usage: build-packages -BuildArch -BuildType"
    echo "BuildArch can be x64, x86, arm, arm64 (default is x64)"
    echo "BuildType can be release, checked, debug (default is debug)"
    echo
    exit 1
}

initHostDistroRid()
{
    __HostDistroRid=""
    if [ "$__HostOS" == "Linux" ]; then
        if [ -e /etc/os-release ]; then
            source /etc/os-release
            if [[ $ID == "rhel" ]]; then
                # remove the last version digit
                VERSION_ID=${VERSION_ID%.*}
            fi
            __HostDistroRid="$ID.$VERSION_ID-$__Arch"
            if [[ $ID == "alpine" ]]; then
                __HostDistroRid="linux-musl-$__Arch"
            fi
        elif [ -e /etc/redhat-release ]; then
            local redhatRelease=$(</etc/redhat-release)
            if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]; then
               __HostDistroRid="rhel.6-$__Arch"
            fi
            if [[ $redhatRelease == "CentOS Linux release 7."* ]]; then
                __HostDistroRid="rhel.7-$__Arch"
            fi
        fi
    fi
    if [ "$__HostOS" == "FreeBSD" ]; then
        __freebsd_version=`sysctl -n kern.osrelease | cut -f1 -d'.'`
        __HostDistroRid="freebsd.$__freebsd_version-$__Arch"
    fi

    if [ "$__HostDistroRid" == "" ]; then
        echo "WARNING: Cannot determine runtime id for current distro."
    fi
}

__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
__IsPortableBuild=1

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        __HostOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        __HostOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        __HostOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        __HostOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        __HostOS=NetBSD
        ;;

    SunOS)
        __BuildOS=SunOS
        __HostOS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        __HostOS=Linux
        ;;
esac

unprocessedBuildArgs=

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    case "$1" in
        -\?|-h|--help)
        usage
        exit 1
        ;;
        -BuildArch=*)
        unprocessedBuildArgs="$unprocessedBuildArgs $1"
        __Arch=$(echo $1| cut -d'=' -f 2)
        ;;

        -portablebuild=false)
            unprocessedBuildArgs="$unprocessedBuildArgs $1"
            __IsPortableBuild=0
            ;;
        *)
        unprocessedBuildArgs="$unprocessedBuildArgs $1"
    esac
    shift
done

# Portable builds target the base RID
if [ $__IsPortableBuild == 1 ]; then
    if [ "$__BuildOS" == "Linux" ]; then
        export __DistroRid="linux-$__Arch"
    elif [ "$__BuildOS" == "OSX" ]; then
        export __DistroRid="osx-$__Arch"
    elif [ "$__BuildOS" == "FreeBSD" ]; then
        export __DistroRid="freebsd-$__Arch"
    fi
else
    # init the host distro name
    initHostDistroRid
    export __DistroRid="$__HostDistroRid"
fi

$__ProjectRoot/run.sh build-packages -Project=$__ProjectRoot/src/.nuget/packages.builds -DistroRid=$__DistroRid -UseSharedCompilation=false -BuildNugetPackage=false -MsBuildEventLogging="/l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll;LogFile=binclash.log" $unprocessedBuildArgs
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred while building packages; See build-packages.log for more details."
    exit 1
fi

echo "Done building packages."
exit 0
