#!/bin/bash

#Usage message
function usage {
    echo 'ARM Emulator Cross Build Script'
    echo 'This script cross builds corefx source'
    echo ''
    echo 'Typical usage:'
    echo '    corefx source is at ~/cfx'
    echo '$ cd ~/cfx'
    echo '$ ./scripts/arm32_ci_script.sh'
    echo '    --buildConfig=Release'
    echo '    --armel'
    echo '    --verbose'
    echo ''
    echo 'Required Arguments:'
    echo '    --buildConfig=<config>             : The value of config should be either Debug or Release'
    echo '                                         Any other value is not accepted'
    echo 'Optional Arguments:'
    echo '    --mode=<mode>                      : docker (default) or emulator'
    echo '    --arm                              : Build as arm (default)'
    echo '    --armel                            : Build as armel'
    echo '    --linuxCodeName=<name>             : Code name for Linux: For arm, trusty (default) and xenial. For armel, tizen'
    echo '    --skipRootFS                       : Skip building rootfs'
    echo '    --emulatorPath=<path>              : Path of the emulator folder (without ending /)'
    echo '                                         <path>/platform/rootfs-t30.ext4 should exist'
    echo '    --mountPath=<path>                 : The desired path for mounting the emulator rootfs (without ending /)'
    echo '                                         This path is created if not already present'
    echo '    -v --verbose                       : Build made verbose'
    echo '    -h --help                          : Prints this usage message and exits'
    echo ''
    echo 'Any other argument triggers an error and this usage message is displayed'
    exit 1
}

#Display error message and exit
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

#Exit if input string is empty
function exit_if_empty {
    local inputString="$1"
    local errorMessage="$2"
    local printUsage=$3

    if [ -z "$inputString" ]; then
        exit_with_error "$errorMessage" $printUsage
    fi
}

#Exit if the input path does not exist
function exit_if_path_absent {
    local path="$1"
    local errorMessage="$2"
    local printUsage=$3

    if [ ! -f "$path" -a ! -d "$path" ]; then
        exit_with_error "$errorMessage" $printUsage
    fi
}

#Check if the git changes were reverted completely
function check_git_head {
    local currentGitHead=`git rev-parse --verify HEAD`

    if [[ "$__initialGitHead" != "$currentGitHead" ]]; then
        exit_with_error "Some changes made to the code history were not completely reverted. Intial Git HEAD: $__initialGitHead, current Git HEAD: $currentGitHead" false
    fi
}

function unmount_rootfs {
    local rootfsFolder="$1"

    #Check if there are any open files in this directory.
    if [ -d $rootfsFolder ]; then
        #If we find information about the file
        if sudo lsof +D $rootfsFolder; then
            (set +x; echo 'See above for lsof information. Continuing with the build.')
        fi
    fi

    if mountpoint -q -- "$rootfsFolder"; then
        sudo umount "$rootfsFolder"
    fi
}

#Unmount the emulator file systems
function unmount_emulator {
    (set +x; echo 'Unmounting emulator...')

    #Unmount all the mounted emulator file systems
    unmount_rootfs "$__ARMRootfsMountPath/proc"
    unmount_rootfs "$__ARMRootfsMountPath/dev/pts"
    unmount_rootfs "$__ARMRootfsMountPath/dev"
    unmount_rootfs "$__ARMRootfsMountPath/run/shm"
    unmount_rootfs "$__ARMRootfsMountPath/sys"
    unmount_rootfs "$__ARMRootfsMountPath"
}

#Clean the changes made to the environment by the script
function clean_env {
    #Check for revert of git changes
    check_git_head
}

#Trap Ctrl-C and handle it
function handle_ctrl_c {
    set +x

    echo 'ERROR: Ctrl-C handled. Script aborted before complete execution.'

    exit 1
}
trap handle_ctrl_c INT

#Trap Exit and handle it
 function handle_exit {
    set +x

    echo 'The script is exited. Cleaning environment..'

    clean_env
 }
trap handle_exit EXIT

#Mount with checking to be already existed
function mount_with_checking {
    set +x
    local options="$1"
    local from="$2"
    local rootfsFolder="$3"

    if mountpoint -q -- "$rootfsFolder"; then
        (set +x; echo "$rootfsFolder is already mounted.")
    else {
        (set -x; sudo mount $options "$from" "$rootfsFolder")
    }
    fi
}

#Mount emulator to the target mount path
function mount_emulator {
    #Check if the mount path exists and create if neccessary
    if [ ! -d "$__ARMRootfsMountPath" ]; then
        sudo mkdir -p "$__ARMRootfsMountPath"
    fi

    set +x
    mount_with_checking "" "$__ARMEmulPath/platform/$__ARMRootfsImageBase" "$__ARMRootfsMountPath"
    mount_with_checking "-t proc" "/proc"    "$__ARMRootfsMountPath/proc"
    mount_with_checking "-o bind" "/dev/"    "$__ARMRootfsMountPath/dev"
    mount_with_checking "-o bind" "/dev/pts" "$__ARMRootfsMountPath/dev/pts"
    mount_with_checking "-t tmpfs" "shm"     "$__ARMRootfsMountPath/run/shm"
    mount_with_checking "-o bind" "/sys"     "$__ARMRootfsMountPath/sys"
}

#Cross builds corefx
function cross_build_corefx {
    #Apply fixes for armel
    if [ "$__buildArch" == "armel" ]; then
        #Export the needed environment variables
        (set +x; echo 'Exporting LINUX_ARM_* environment variable')
        source "$__ARMRootfsMountPath"/dotnet/setenv/setenv_incpath.sh "$__ARMRootfsMountPath"
    fi

    #Cross building for emulator rootfs
    ROOTFS_DIR="$__ARMRootfsMountPath" CPLUS_INCLUDE_PATH=$LINUX_ARM_INCPATH CXXFLAGS=$LINUX_ARM_CXXFLAGS ./src/Native/build-native.sh $__buildArch $__buildConfig cross $__verboseFlag

    ROOTFS_DIR="$__ARMRootfsMountPath" CPLUS_INCLUDE_PATH=$LINUX_ARM_INCPATH CXXFLAGS=$LINUX_ARM_CXXFLAGS ./build.sh -$__buildConfig /p:BuildNative=false /p:SkipTests=true /p:BuildPackages=false
}

# Cross builds corefx using Docker image
function cross_build_corefx_with_docker {
    __currentWorkingDirectory=`pwd`

    # Check build configuration and choose Docker image
    if [ "$__buildArch" == "arm" ]; then
        # TODO: For arm, we are going to embed RootFS inside Docker image.
        case $__linuxCodeName in
        trusty)
            __dockerImage=" microsoft/dotnet-buildtools-prereqs:ubuntu-14.04-cross-0cd4667-20172211042239"
            __skipRootFS=1
            __dockerEnvironmentVariables="-e ROOTFS_DIR=/crossrootfs/arm"
            __runtimeOS="ubuntu.14.04"
        ;;
        xenial)
            __dockerImage=" microsoft/dotnet-buildtools-prereqs:ubuntu-16.04-cross-ef0ac75-20175511035548"
            __skipRootFS=1
            __dockerEnvironmentVariables="-e ROOTFS_DIR=/crossrootfs/arm"
            __runtimeOS="ubuntu.16.04"
        ;;
        *)
            exit_with_error "ERROR: $__linuxCodeName is not a supported linux name for $__buildArch" false
        ;;
        esac
    elif [ "$__buildArch" == "armel" ]; then
        # For armel Tizen, we are going to construct RootFS on the fly.
        case $__linuxCodeName in
        tizen)
            __dockerImage=" tizendotnet/dotnet-buildtools-prereqs:ubuntu-16.04-cross-e435274-20180426002255-tizen-rootfs-5.0m1"
            __skipRootFS=1
            __dockerEnvironmentVariables+=" -e ROOTFS_DIR=/crossrootfs/armel.tizen.build"
            __runtimeOS="tizen.5.0.0"
        ;;
        *)
            echo "ERROR: $__linuxCodeName is not a supported linux name for $__buildArch"
            exit_with_error "ERROR: $__linuxCodeName is not a supported linux name for $__buildArch" false
        ;;
        esac
    else
        exit_with_error "ERROR: unknown buildArch $__buildArch" false
    fi
    __dockerCmd="sudo docker run ${__dockerEnvironmentVariables} --privileged -i --rm -v $__currentWorkingDirectory:/opt/corefx -w /opt/corefx $__dockerImage"

    if [ $__skipRootFS == 0 ]; then
        # Build rootfs
        __buildRootfsCmd="./cross/build-rootfs.sh $__buildArch $__linuxCodeName --skipunmount"

        (set +x; echo "Build RootFS for $__buildArch $__linuxCodeName")
        $__dockerCmd $__buildRootfsCmd
        sudo chown -R $(id -u -n) cross/rootfs
    fi

    # Cross building corefx with rootfs in Docker
    if [ "$__buildArch" == "armel" ]; then
        __extraCmd="/p:OverridePackageSource=https://tizen.myget.org/F/dotnet-core/api/v3/index.json"
        __portableLinux="/p:PortableBuild=false"
    fi

    __buildCmd="./build.sh /p:ArchGroup=$__buildArch -$__buildConfig /p:RuntimeOS=$__runtimeOS $__portableLinux $__extraCmd"
    $__dockerCmd $__buildCmd
    sudo chown -R $(id -u -n) ./artifacts
}

#Define script variables
__ciMode="docker"
__ARMEmulPath=
__ARMRootfsImageBase="rootfs-u1404.ext4"
__ARMRootfsMountPath=
__buildConfig=
__verboseFlag=
__buildArch="arm"
__linuxCodeName="trusty"
__skipRootFS=0
__initialGitHead=`git rev-parse --verify HEAD`

#Parse command line arguments
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
        __buildConfig="$(echo ${arg#*=} | awk '{print tolower($0)}')"
        if [[ "$__buildConfig" != "debug" && "$__buildConfig" != "release" ]]; then
            exit_with_error "--buildConfig can be only Debug or Release" true
        fi
        ;;
    --mode=*)
        __ciMode=${arg#*=}
        ;;
    --arm)
        __ARMRootfsImageBase="rootfs-u1404.ext4"
        __buildArch="arm"
        ;;
    --armel)
        __ARMRootfsImageBase="rootfs-t30.ext4"
        __buildArch="armel"
        __linuxCodeName="tizen"
        ;;
    --linuxCodeName=*)
        __linuxCodeName=${arg#*=}
        ;;
    --skipRootFS)
        __skipRootFS=1
        ;;
    -v|--verbose)
        __verboseFlag="verbose"
        ;;
    -h|--help)
        usage
        ;;
    *)
        exit_with_error "$arg not a recognized argument" true
        ;;
    esac
done

#Check if there are any uncommited changes in the source directory as git adds and removes patches
if [[ $(git status -s) != "" ]]; then
   echo 'ERROR: There are some uncommited changes. To avoid losing these changes commit them and try again.'
   echo ''
   git status
   exit 1
fi

exit_if_empty "$__buildConfig" "--buildConfig is a mandatory argument, not provided" true
if [ "$__ciMode" == "emulator" ]; then
    #Check if the compulsory arguments have been presented to the script and if the input paths exist
    exit_if_empty "$__ARMEmulPath" "--emulatorPath is a mandatory argument, not provided" true
    exit_if_empty "$__ARMRootfsMountPath" "--mountPath is a mandatory argument, not provided" true
    exit_if_path_absent "$__ARMEmulPath/platform/$__ARMRootfsImageBase" "Path specified in --emulatorPath does not have the rootfs" false

    __ARMRootfsMountPath="${__ARMRootfsMountPath}_${__buildArch}"
fi

set -x
set -e

## Begin cross build
(set +x; echo "Git HEAD @ $__initialGitHead")

#Complete the cross build
(set +x; echo 'Building corefx...')
if [ "$__ciMode" == "docker" ]; then
    cross_build_corefx_with_docker
else
    cross_build_corefx
fi

#Clean the environment
(set +x; echo 'Cleaning environment...')
clean_env

(set +x; echo 'Build complete')
