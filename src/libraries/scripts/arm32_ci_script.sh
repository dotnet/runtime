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
    echo '    --emulatorPath=/opt/linux-arm-emulator'
    echo '    --mountPath=/opt/linux-arm-emulator-root'
    echo '    --buildConfig=Release'
    echo '    --softfp'
    echo '    --verbose'
    echo ''
    echo 'Required Arguments:'
    echo '    --emulatorPath=<path>              : Path of the emulator folder (without ending /)'
    echo '                                         <path>/platform/rootfs-t30.ext4 should exist'
    echo '    --mountPath=<path>                 : The desired path for mounting the emulator rootfs (without ending /)'
    echo '                                         This path is created if not already present'
    echo '    --buildConfig=<config>             : The value of config should be either Debug or Release'
    echo '                                         Any other value is not accepted'
    echo 'Optional Arguments:'
    echo '    --softfp                           : Build as arm-softfp'
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
    #Apply fixes for softfp 
    if [ "$__buildArch" == "arm-softfp" ]; then
        #Export the needed environment variables
        (set +x; echo 'Exporting LINUX_ARM_* environment variable')
        source "$__ARMRootfsMountPath"/dotnet/setenv/setenv_incpath.sh "$__ARMRootfsMountPath"

        #Apply the changes needed to build for the emulator rootfs
        (set +x; echo 'Applying cross build patch to suit Linux ARM emulator rootfs')
        git am < "$__ARMRootfsMountPath"/dotnet/setenv/corefx_cross.patch
    fi

    #Cross building for emulator rootfs
    ROOTFS_DIR="$__ARMRootfsMountPath" CPLUS_INCLUDE_PATH=$LINUX_ARM_INCPATH CXXFLAGS=$LINUX_ARM_CXXFLAGS ./build-native.sh -buildArch=$__buildArch -$__buildConfig -- cross $__verboseFlag
    ROOTFS_DIR="$__ARMRootfsMountPath" CPLUS_INCLUDE_PATH=$LINUX_ARM_INCPATH CXXFLAGS=$LINUX_ARM_CXXFLAGS ./build-managed.sh -$__buildConfig -skipTests

    #Reset the code to the upstream version
    (set +x; echo 'Rewinding HEAD to master code')
    git reset --hard HEAD^
}

#Define script variables
__ARMEmulPath=
__ARMRootfsImageBase="rootfs-u1404.ext4"
__ARMRootfsMountPath=
__buildConfig=
__verboseFlag=
__buildArch="arm"
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
    --softfp)
        __ARMRootfsImageBase="rootfs-t30.ext4"
        __buildArch="arm-softfp"
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

#Check if the compulsory arguments have been presented to the script and if the input paths exist
exit_if_empty "$__ARMEmulPath" "--emulatorPath is a mandatory argument, not provided" true
exit_if_empty "$__ARMRootfsMountPath" "--mountPath is a mandatory argument, not provided" true
exit_if_empty "$__buildConfig" "--buildConfig is a mandatory argument, not provided" true
exit_if_path_absent "$__ARMEmulPath/platform/$__ARMRootfsImageBase" "Path specified in --emulatorPath does not have the rootfs" false

__ARMRootfsMountPath="${__ARMRootfsMountPath}_${__buildArch}"

set -x
set -e

## Begin cross build
(set +x; echo "Git HEAD @ $__initialGitHead")

#Mount the emulator
(set +x; echo 'Mounting emulator...')
mount_emulator

#Complete the cross build
(set +x; echo 'Building corefx...')
cross_build_corefx

#Clean the environment
(set +x; echo 'Cleaning environment...')
clean_env

(set +x; echo 'Build complete')
