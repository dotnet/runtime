#!/bin/bash

set -x

function usage {
    echo 'ARM Test Script'
    echo '$ ./tests/scripts/arm32_ci_test.sh'
    echo '    --abi=arm'
    echo '    --buildConfig=Release'
    echo 'Required Arguments:'
    echo '    --abi=<abi>                        : arm (default) or armel'
    echo '    --buildConfig=<config>             : Release (default) Checked, or Debug'
}

# Display error message and exit
function exit_with_error {
    set +x

    local errorMessage="$1"
    local printUsage=$2

    echo "ERROR: $errorMessage"
    if [ "$printUsage" == "true" ]; then
        echo ''
        usage
    fi
    exit 1
}

# Exit if the input path does not exist
function exit_if_path_absent {
    local path="$1"
    local errorMessage="$2"
    local printUsage=$3

    if [ ! -f "$path" -a ! -d "$path" ]; then
        exit_with_error "$errorMessage" $printUsage
    fi
}

__abi="arm"
__buildConfig="Release"

# Parse command line arguments
for arg in "$@"
do
    case $arg in
    --abi=*)
        __abi=${arg#*=}
        if [[ "$__abi" != "arm" && "$__abi" != "armel" ]]; then
            exit_with_error "--abi can be either arm or armel" true
        fi
        ;;
    --buildConfig=*)
        __buildConfig=${arg#*=}
        if [[ "$__buildConfig" != "Debug" && "$__buildConfig" != "Release" && "$__buildConfig" != "Checked" ]]; then
            exit_with_error "--buildConfig can be Debug, Checked or Release" true
        fi
        ;;
    -v|--verbose)
        __verboseFlag="verbose"
        ;;
    -h|--help)
        usage
        exit 0
        ;;
    *)
        exit_with_error "$arg not a recognized argument" true
        ;;
    esac
done
__buildDirName="Linux.${__abi}.${__buildConfig}"

CORECLR_DIR=/opt/code
ARM_CHROOT_HOME_DIR=/home/coreclr

if [ -z "${ROOTFS_DIR}" ]; then
    __ROOTFS_DIR=${CORECLR_DIR}/cross/rootfs/${__abi}
else
    __ROOTFS_DIR=${ROOTFS_DIR}
fi

if [ "$__abi" == "armel" ]; then
    # Prepare armel emulation environment
    pushd ${CORECLR_DIR}/cross/armel/tizen
    apt-get update
    apt-get -y -qq --force-yes --reinstall install qemu binfmt-support qemu-user-static
    __qemuARM=$(which qemu-arm-static)
    cp $__qemuARM ${__ROOTFS_DIR}/usr/bin/
    popd
fi

# Mount
mkdir -p ${__ROOTFS_DIR}${ARM_CHROOT_HOME_DIR}
mount -t proc /proc ${__ROOTFS_DIR}/proc
mount -o bind /dev ${__ROOTFS_DIR}/dev
mount -o bind /dev/pts ${__ROOTFS_DIR}/dev/pts
mount -o bind /sys ${__ROOTFS_DIR}/sys
mount -o bind ${CORECLR_DIR} ${__ROOTFS_DIR}${ARM_CHROOT_HOME_DIR}

# Test environment emulation using docker and qemu has some problem to use lttng library.
# We should remove libcoreclrtraceptprovider.so to avoid test hang.
rm -f -v ${__ROOTFS_DIR}${ARM_CHROOT_HOME_DIR}/bin/Product/${__buildDirName}/libcoreclrtraceptprovider.so
rm -f -v ${__ROOTFS_DIR}${ARM_CHROOT_HOME_DIR}/bin/CoreFxBinDir/libcoreclrtraceptprovider.so

chroot ${__ROOTFS_DIR} /bin/bash -x <<EOF
    cd ${ARM_CHROOT_HOME_DIR}
    ./tests/bringup_runtest.sh --sequential\
                       --coreClrBinDir=${ARM_CHROOT_HOME_DIR}/bin/Product/${__buildDirName} \
                       --mscorlibDir=${ARM_CHROOT_HOME_DIR}/bin/Product/${__buildDirName} \
                       --testNativeBinDir=${ARM_CHROOT_HOME_DIR}/bin/obj/${__buildDirName}/tests \
                       --coreFxBinDir=${ARM_CHROOT_HOME_DIR}/bin/CoreFxBinDir \
                       --testRootDir=${ARM_CHROOT_HOME_DIR}/bin/tests/Windows_NT.x64.${__buildConfig} \
                       --testDirFile=${ARM_CHROOT_HOME_DIR}/tests/testsRunningInsideARM.txt
EOF
