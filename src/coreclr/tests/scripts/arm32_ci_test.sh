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
    # TODO: Make use of a single Tizen rootfs for build and test

    # TODO-cleanup: the latest docker image already has mic installed.
    # Prepare Tizen (armel) environment
    #echo "deb http://download.tizen.org/tools/latest-release/Ubuntu_14.04 /" >> /etc/apt/sources.list
    #apt-get update
    #apt-get -y -qq --force-yes install mic

    pushd ${CORECLR_DIR}/cross/armel/tizen
    mic --non-interactive create fs --pack-to=tizen.tar.gz tizen-dotnet.ks
    if [ -d ${__ROOTFS_DIR} ]; then
        mv ${__ROOTFS_DIR} ${__ROOTFS_DIR}_build
    fi
    mkdir -p ${__ROOTFS_DIR}
    tar -zxf mic-output/tizen.tar.gz -C ${__ROOTFS_DIR}
    apt-get update
    apt-get -y -qq --force-yes install --reinstall qemu binfmt-support qemu-user-static
    __qemuARM=$(which qemu-arm-static)
    cp $__qemuARM ${CORECLR_DIR}/cross/rootfs/armel/usr/bin/
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

chroot ${__ROOTFS_DIR} /bin/bash -x <<EOF
    cd ${ARM_CHROOT_HOME_DIR}
    ./tests/runtest.sh --sequential\
                       --coreClrBinDir=${ARM_CHROOT_HOME_DIR}/bin/Product/${__buildDirName} \
                       --mscorlibDir=${ARM_CHROOT_HOME_DIR}/bin/Product/${__buildDirName} \
                       --testNativeBinDir=${ARM_CHROOT_HOME_DIR}/bin/obj/${__buildDirName}/tests \
                       --coreFxBinDir=${ARM_CHROOT_HOME_DIR}/bin/CoreFxBinDir \
                       --testRootDir=${ARM_CHROOT_HOME_DIR}/bin/tests/Windows_NT.x64.${__buildConfig} \
                       --testDirFile=${ARM_CHROOT_HOME_DIR}/tests/testsRunningInsideARM.txt
EOF
