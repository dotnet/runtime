#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [BuildArch] [UbuntuCodeName]"
    echo "BuildArch can be: arm, arm-softfp, arm64"
    echo "UbuntuCodeName - optional, Code name for Ubuntu, can be: trusty(default), vivid, wily. If BuildArch is arm-softfp, UbuntuCodeName is ignored."

    exit 1
}

__UbuntuCodeName=trusty

__CrossDir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
__InitialDir=$PWD
__BuildArch=arm
__UbuntuArch=armhf
__UbuntuRepo="http://ports.ubuntu.com/"
__UbuntuPackagesBase="build-essential libunwind8-dev gettext symlinks liblttng-ust-dev libicu-dev"
__UbuntuPackages="$__UbuntuPackagesBase"
if [ -z "$LLVM_ARM_HOME" ]; then
    __LLDB_Package="lldb-3.6-dev"
fi
__MachineTriple=arm-linux-gnueabihf
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
        __UbuntuPackages="$__UbuntuPackagesBase $__LLDB_Package"
        __MachineTriple=arm-linux-gnueabihf
        ;;
        arm64)
        __BuildArch=arm64
        __UbuntuArch=arm64
        __UbuntuPackages="$__UbuntuPackagesBase"
        __MachineTriple=aarch64-linux-gnu
        ;;
        arm-softfp)
        __BuildArch=arm-softfp
        __UbuntuArch=armel
        __UbuntuRepo="http://ftp.debian.org/debian/"
        __UbuntuPackages="$__UbuntuPackagesBase $__LLDB_Package"
        __MachineTriple=arm-linux-gnueabi
        __UbuntuCodeName=jessie
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
        *)
        __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
        ;;
    esac
done

__RootfsDir="$__CrossDir/rootfs/$__BuildArch"

if [[ -n "$ROOTFS_DIR" ]]; then
    __RootfsDir=$ROOTFS_DIR
fi

umount $__RootfsDir/*
rm -rf $__RootfsDir
qemu-debootstrap --arch $__UbuntuArch $__UbuntuCodeName $__RootfsDir $__UbuntuRepo
cp $__CrossDir/$__BuildArch/sources.list.$__UbuntuCodeName $__RootfsDir/etc/apt/sources.list
chroot $__RootfsDir apt-get update
chroot $__RootfsDir apt-get -f -y install
chroot $__RootfsDir apt-get -y install $__UbuntuPackages
chroot $__RootfsDir symlinks -cr /usr
umount $__RootfsDir/*
