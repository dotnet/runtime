#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [BuildArch] [UbuntuCodeName] [lldbx.y]"
    echo "BuildArch can be: arm(default), armel, arm64, x86"
    echo "UbuntuCodeName - optional, Code name for Ubuntu, can be: trusty(default), vivid, wily, xenial. If BuildArch is armel, UbuntuCodeName is ignored."
    echo "lldbx.y - optional, LLDB version, can be: lldb3.6(default), lldb3.8"

    exit 1
}

__UbuntuCodeName=trusty

__CrossDir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
__InitialDir=$PWD
__BuildArch=arm
__UbuntuArch=armhf
__UbuntuRepo="http://ports.ubuntu.com/"
__UbuntuPackagesBase="build-essential libunwind8-dev gettext symlinks liblttng-ust-dev libicu-dev"
__LLDB_Package="lldb-3.6-dev"
__UnprocessedBuildArgs=

for i in "$@"
    do
        lowerI="$(echo $i | awk '{print tolower($0)}')"
        case $lowerI in
        -?|-h|--help)
            usage
            exit 1
            ;;
        arm)
            __BuildArch=arm
            __UbuntuArch=armhf
            ;;
        arm64)
            __BuildArch=arm64
            __UbuntuArch=arm64
            ;;
        armel)
            __BuildArch=armel
            __UbuntuArch=armel
            __UbuntuRepo="http://ftp.debian.org/debian/"
            __UbuntuCodeName=jessie
            ;;
        x86)
            __BuildArch=x86
            __UbuntuArch=i386
            __UbuntuRepo="http://archive.ubuntu.com/ubuntu/"
            ;;
        lldb3.6)
            __LLDB_Package="lldb-3.6-dev"
            ;;
        lldb3.8)
            __LLDB_Package="lldb-3.8-dev"
            ;;
        vivid)
            if [ "$__UbuntuCodeName" != "jessie" ]; then
                __UbuntuCodeName=vivid
            fi
            ;;
        wily)
            if [ "$__UbuntuCodeName" != "jessie" ]; then
                __UbuntuCodeName=wily
            fi
            ;;
        xenial)
            if [ "$__UbuntuCodeName" != "jessie" ]; then
                __UbuntuCodeName=xenial
            fi
            ;;
        jessie)
            __UbuntuCodeName=jessie
            __UbuntuRepo="http://ftp.debian.org/debian/"
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
            ;;
    esac
done

if [ "$__BuildArch" == "armel" ]; then
     __LLDB_Package="lldb-3.5-dev"
fi

__RootfsDir="$__CrossDir/rootfs/$__BuildArch"
__UbuntuPackages="$__UbuntuPackagesBase $__LLDB_Package"

if [[ -n "$ROOTFS_DIR" ]]; then
    __RootfsDir=$ROOTFS_DIR
fi

if [ -d "$__RootfsDir" ]; then
    umount $__RootfsDir/*
    rm -rf $__RootfsDir
fi

qemu-debootstrap --arch $__UbuntuArch $__UbuntuCodeName $__RootfsDir $__UbuntuRepo
cp $__CrossDir/$__BuildArch/sources.list.$__UbuntuCodeName $__RootfsDir/etc/apt/sources.list
chroot $__RootfsDir apt-get update
chroot $__RootfsDir apt-get -f -y install
chroot $__RootfsDir apt-get -y install $__UbuntuPackages
chroot $__RootfsDir symlinks -cr /usr
umount $__RootfsDir/*
