#!/bin/bash

usage() {
	echo "Usage: $0 <emulator folder path> <rootfs mount path> <build configuration>"
	echo "		emulator folder path: The path to the emulator directory"
	echo "		rootfs mount path: The target path for mounting the emulator rootfs"
	echo "				   This path needs to exist, else the mount command will fail"
	echo "		build configuration: Debug or Release (argument is must)"
	echo "	     Both the emulator directory path and the target mount path should not end in /"
	echo "	     This script should be called from inside the coreclr source directory"
	echo "	     All 3 arguments are a must"
	exit 1
}

if [ $# -ne 3 ]
then
	usage
fi

set -x

armemul_path=$1 ; armrootfs_mountpath=$2 ; build_config=$3

if [ ! -d $armrootfs_mountpath ]; then sudo mkdir $armrootfs_mountpath; fi;

if grep -qs $armrootfs_mountpath /proc/mounts; then sudo umount $armrootfs_mountpath; fi ; sudo mount $armemul_path/platform/rootfs-t30.ext4 $armrootfs_mountpath

echo "Exporting LINUX_ARM_INCPATH environment variable"
source $armrootfs_mountpath/dotnet/setenv/setenv_incpath.sh $armrootfs_mountpath

echo "Applying cross build patch to suit Linux ARM emulator rootfs"
git am < $armrootfs_mountpath/dotnet/setenv/coreclr_cross.patch

ROOTFS_DIR=$armrootfs_mountpath CPLUS_INCLUDE_PATH=$LINUX_ARM_INCPATH CXXFLAGS=$LINUX_ARM_CXXFLAGS ./build.sh arm-softfp clean cross verbose skipmscorlib clang3.5 $build_config

echo "Rewinding HEAD to master code"
git reset --hard HEAD^
