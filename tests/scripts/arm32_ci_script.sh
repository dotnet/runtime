#!/bin/bash

usage() {
    echo 'ARM Emulator Cross Build Script'
    echo ''
    echo 'Typical usage:'
    echo './tests/scripts/arm32_ci_script.sh'
    echo '    --emulatorPath=/opt/linux-arm-emulator'
    echo '    --mountPath=/opt/linux-arm-emulator-root'
    echo '    --buildConfig=Release'
    echo ''
    echo 'Required Arguments:'
    echo ''
    echo '    --emulatorPath=<path>    Path of the emulator folder (without ending /)'
    echo '                             <path>/platform/rootfs-t30.ext4 should exist'
    echo '    --mountPath=<path>       The desired path for mounting the emulator rootfs (without ending /)'
    echo '                             This path is created if not already present'
    echo '    --buildConfig=<config>   The value of config should be either Debug or Release'
    echo '                             Any other value is not accepted'
    echo ''
    echo 'Any other argument triggers an error and this message is displayed'
    exit 1
}

__ARMEmulPath=
__ARMRootfsMountPath=
__BuildConfig=

for arg in "$@"
do
    case $arg in
    --emulatorPath=*)
        __ARMEmulPath=${arg#*=}
        ;;
    --mountPath=*)
        __ARMRootfsMountPath=${arg#*=}
        ;;
    --buildConfig=*)
        __BuildConfig="$(echo ${arg#*=} | awk '{print tolower($0)}')"
        if [[ "$__BuildConfig" != "debug" && "$__BuildConfig" != "release" ]]; then
            usage
        fi
        ;;
    *)
        usage
        ;;
    esac
done

if [ -z "$__ARMEmulPath" -o -z "$__ARMRootfsMountPath" -o -z "$__BuildConfig" ]; then
    usage
fi

set -x
set -e

if [ ! -d $__ARMRootfsMountPath ]; then
    sudo mkdir $__ARMRootfsMountPath
fi

if grep -qs $__ARMRootfsMountPath /proc/mounts; then
    sudo umount $__ARMRootfsMountPath
fi

sudo mount $__ARMEmulPath/platform/rootfs-t30.ext4 $__ARMRootfsMountPath

echo "Exporting LINUX_ARM_* environment variable"
source $__ARMRootfsMountPath/dotnet/setenv/setenv_incpath.sh $__ARMRootfsMountPath

echo "Applying cross build patch to suit Linux ARM emulator rootfs"
git am < $__ARMRootfsMountPath/dotnet/setenv/coreclr_cross.patch

ROOTFS_DIR=$__ARMRootfsMountPath CPLUS_INCLUDE_PATH=$LINUX_ARM_INCPATH CXXFLAGS=$LINUX_ARM_CXXFLAGS ./build.sh arm-softfp clean cross verbose skipmscorlib clang3.5 $__BuildConfig

echo "Rewinding HEAD to master code"
git reset --hard HEAD^
