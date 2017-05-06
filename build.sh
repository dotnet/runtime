#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Set OFFLINE environment variable to build offline

set -e

initHostDistroRid()
{
    if [ "$__HostOS" == "Linux" ]; then
        if [ ! -e /etc/os-release ]; then
            echo "WARNING: Can not determine runtime id for current distro."
            __HostDistroRid=""
        else
            source /etc/os-release
            __HostDistroRid="$ID.$VERSION_ID-$__HostArch"
        fi
    fi
}

initTargetDistroRid()
{
    if [ $__CrossBuild == 1 ]; then
        if [ "$__BuildOS" == "Linux" ]; then
            if [ ! -e $ROOTFS_DIR/etc/os-release ]; then
                echo "WARNING: Can not determine runtime id for current distro."
                export __DistroRid=""
            else
                source $ROOTFS_DIR/etc/os-release
                export __DistroRid="$ID.$VERSION_ID-$__BuildArch"
            fi
        fi
    else
        export __DistroRid="$__HostDistroRid"
    fi

    # Portable builds target the base RID
    if [ $__PortableBuild == 1 ]; then
        if [ "$__BuildOS" == "Linux" ]; then
            export __DistroRid="linux-$__BuildArch"
        elif [ "$__BuildOS" == "OSX" ]; then
            export __DistroRid="osx-$__BuildArch"
        fi
    fi
}


SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

# Some things depend on HOME and it may not be set. We should fix those things, but until then, we just patch a value in
if [ -z "$HOME" ] || [ ! -d "$HOME" ]; then
    export HOME=$DIR/Bin/home

    [ ! -d "$HOME" ] || rm -Rf $HOME
    mkdir -p $HOME
fi


# Use uname to determine what the CPU is.

CPUName=$(uname -p)

# Some Linux platforms report unknown for platform, but the arch for machine.

if [ "$CPUName" == "unknown" ]; then

    CPUName=$(uname -m)

fi



case $CPUName in
    i686)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=x86
        __HostArch=x86
        ;;

    x86_64)
        __BuildArch=x64
        __HostArch=x64
        ;;

    armv7l)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=arm
        __HostArch=arm
        ;;

    aarch64)
        __BuildArch=arm64
        __HostArch=arm64
        ;;

    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        __BuildArch=x64
        __HostArch=x64
        ;;
esac

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

__BuildType=Debug
__BuildArch=x64
__SkipTests=false
__DisableCrossgen=false
__VerboseBuild=0
__CrossBuild=0
__PortableBuild=0

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -\?|-h|--help)
            usage
            exit 1
            ;;

        x86)
            __BuildArch=x86
            ;;

        x64)
            __BuildArch=x64
            ;;

        arm)
            __BuildArch=arm
            __DisableCrossgen=true
            ;;

        armel)
            __BuildArch=armel
            __DisableCrossgen=true
            ;;

        arm64)
            __BuildArch=arm64
            __DisableCrossgen=true
            ;;

        debug)
            __BuildType=Debug
            ;;

        release)
            __BuildType=Release
            ;;

        cross)
            __CrossBuild=1
            ;;
            
        -portable)
            __PortableBuild=1
            ;;

        verbose)
            __VerboseBuild=1
            ;;

        skiptests)
            __SkipTests=true
            ;;
        disablecrossgen)
            __DisableCrossgen=true
            ;;
    esac

    shift
done

# init the host distro name
initHostDistroRid

# init the target distro name
initTargetDistroRid

__RunArgs="-TargetArchitecture=$__BuildArch -ConfigurationGroup=$__BuildType -OSGroup=$__HostOS -DistroRid=$__DistroRid -SkipTests=$__SkipTests -DisableCrossgen=$__DisableCrossgen"

if [ $__PortableBuild == 1 ]; then
  __RunArgs="$__RunArgs -PortableBuild=True"
fi

# Configure environment if we are doing a verbose build
if [ $__VerboseBuild == 1 ]; then
    export VERBOSE=1
	__RunArgs="$__RunArgs -verbose"
fi

echo "$__RunArgs"
$DIR/run.sh build $__RunArgs