#!/usr/bin/env bash

usage()
{
    echo "Builds the NuGet packages from the binaries that were built in the Build product binaries step."
    echo "Usage: build-packages -BuildArch -BuildType [portableLinux]"
    echo "BuildArch can be x64, x86, arm, arm64 (default is x64)"
    echo "BuildType can be release, checked, debug (default is debug)"
    echo "portableLinux - build for Portable Linux Distribution"
    echo
    exit 1
}

__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
__PortableLinux=0

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

        portableLinux)
            if [ "$__BuildOS" == "Linux" ]; then
                __PortableLinux=1
            else
                echo "ERROR: portableLinux not supported for non-Linux platforms."
                exit 1
            fi
            ;;
        *)
        unprocessedBuildArgs="$unprocessedBuildArgs $1"
    esac
    shift
done

# Portable builds target the base RID only for Linux based platforms
if [ $__PortableLinux == 1 ]; then
    export __DistroRid="linux-$__Arch"
else
    export __DistroRid="\${OSRid}-$__Arch"
fi

$__ProjectRoot/run.sh build-packages -Project=$__ProjectRoot/src/.nuget/packages.builds -DistroRid=$__DistroRid -UseSharedCompilation=false -BuildNugetPackage=false $unprocessedBuildArgs
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred while building packages; See build-packages.log for more details."
    exit 1
fi

echo "Done building packages."
exit 0
