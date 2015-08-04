#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [BuildArch]"
    echo "BuildArch can be: arm, arm64"

    exit 1
}

fix_symlinks()
{
    echo "Adjusting the symlinks in $1 to be relative"
    cd $1
    find . -maxdepth 1 -type l | while read i
    do qualifies=$(file $i | sed -e "s/.*\`\(.*\)'/\1/g" | grep ^/lib)
        if [ -n "$qualifies" ]; then
            newPath=$(file $i | sed -e "s/.*\`\(.*\)'/\1/g" | sed -e "s,\`,,g" | sed -e "s,',,g" | sed -e "s,^/lib,$2/lib,g")
            echo $i
            echo $newPath
            sudo rm $i
            sudo ln -s $newPath $i
        fi
    done
    cd $__InitialDir
}

__CrossDir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
__InitialDir=$PWD
__BuildArch=arm
__UbuntuArch=armhf
__UbuntuRepo="http://ports.ubuntu.com/"
__UbuntuPackages="build-essential lldb-3.6-dev libunwind8-dev gettext"
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
        __UbuntuRepo="http://ports.ubuntu.com/"
        __UbuntuPackages="build-essential lldb-3.6-dev libunwind8-dev gettext"
        __MachineTriple=arm-linux-gnueabihf
        ;;
        arm64)
        __BuildArch=arm64
        __UbuntuArch=arm64
        __UbuntuRepo="http://ports.ubuntu.com/"
        __UbuntuPackages="build-essential libunwind8-dev gettext"
        __MachineTriple=aarch64-linux-gnu
        ;;
        *)
        __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
    esac
done

__RootfsDir="$__CrossDir/rootfs/$__BuildArch"

if [[ -n "$ROOTFS_DIR" ]]; then
    __RootfsDir=$ROOTFS_DIR
fi

__RootfsDirMultiArchDir=$__RootfsDir/usr/lib/$__MachineTriple
# On ARM64 libgcc_s.so is a symlink to /lib/ so we need to correct that too
__RootfsDirGCCMultiArchDir=$__RootfsDir/usr/lib/gcc/$__MachineTriple/4.8

umount $__RootfsDir/*
rm -rf $__RootfsDir
qemu-debootstrap --arch $__UbuntuArch trusty $__RootfsDir $__UbuntuRepo
cp $__CrossDir/$__BuildArch/sources.list $__RootfsDir/etc/apt/sources.list
chroot $__RootfsDir apt-get update
chroot $__RootfsDir apt-get -y install $__UbuntuPackages
umount $__RootfsDir/*
fix_symlinks $__RootfsDir/usr/lib "../.."
if [ -d "$__RootfsDirMultiArchDir" ]; then
    fix_symlinks $__RootfsDirMultiArchDir "../../.."
fi
if [ -d "$__RootfsDirGCCMultiArchDir" ]; then
    fix_symlinks $__RootfsDirGCCMultiArchDir "../../../../.."
fi