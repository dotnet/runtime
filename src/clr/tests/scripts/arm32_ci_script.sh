#!/bin/bash

#Usage message
function usage {
    echo 'ARM Emulator Cross Build and Test Script'
    echo 'This script cross builds coreclr source and tests the binaries generated'
    echo ''
    echo 'Typical usage:'
    echo '    coreclr source is at ~/clr'
    echo '    corefx source is at ~/cfx'
    echo '    --testRootDir and --mscorlibDir have been built on Windows/downloaded from dotnet-ci.cloudapp.net'
    echo '    --coreFxNativeBinDir has been built using cross build'
    echo '    --coreFxBinDir has been built on Linux'
    echo '$ cd ~/clr'
    echo '$ ./tests/scripts/arm32_ci_script.sh'
    echo '    --emulatorPath=/opt/linux-arm-emulator'
    echo '    --mountPath=/opt/linux-arm-emulator-root'
    echo '    --buildConfig=Release'
    echo '    --testRootDir=~/Downloads/Windows_NT.x64.Release'
    echo '    --mscorlibDir=~/clr/bin/Product/Linux.arm-softfp.Release'
    echo '    --coreFxNativeBinDir=~/cfx/bin/Linux.arm-softfp.Release'
    echo '    --coreFxBinDir="~/cfx/bin/Linux.AnyCPU.Release;~/cfx/bin/Unix.AnyCPU.Release;~/cfx/bin/AnyOS.AnyCPU.Release"'
    echo '    --testDirFile=~/clr/tests/testsRunningInsideARM.txt'
    echo ''
    echo 'Required Arguments:'
    echo '    --emulatorPath=<path>              : Path of the emulator folder (without ending /)'
    echo '                                         <path>/platform/rootfs-t30.ext4 should exist'
    echo '    --mountPath=<path>                 : The desired path for mounting the emulator rootfs (without ending /)'
    echo '                                         This path is created if not already present'
    echo '    --buildConfig=<config>             : The value of config should be either Debug or Release'
    echo '                                         Any other value is not accepted'
    echo 'Optional Arguments:'
    echo '    --skipTests                        : Presenting this option skips testing the generated binaries'
    echo '                                         If this option is not presented, then tests are run by default'
    echo '                                         using the other test related options'
    echo '    --skipmscorlib                     : Skips generating mscorlib.dll on Linux'
    echo '                                         If tests are run and this option is not used,'
    echo '                                         then --mscorlibDir option to this script is mandatory'
    echo '    -v --verbose                       : Build made verbose'
    echo '    -h --help                          : Prints this usage message and exits'
    echo ''
    echo 'Test related Arguments (mandatory if --skipTests is not used):'
    echo '    --testRootDir=<path>               : The root directory of the test build'
    echo '    --mscorlibDir=<path>               : The directory containing the mscorlib.dll binary'
    echo '                                         If provided, then the mscorlib.dll in this directory is'
    echo '                                         used for tests instead of the built mscorlib.dll'
    echo '    --coreFxNativeBinDir=<path>        : The directory of the CoreFX native build'
    echo '    --coreFxBinDir="<path>[;<path>]"   : List one or more directories with CoreFX managed build binaries'
    echo '    --testDirFile=<path>               : Runs tests only in the directories specified by the file at <path>'
    echo '                                         The directories are listed in lines in the file at <path>'
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

#Clean the previous build files inside the emulator
function clean_emulator {
    #Remove any previous copies of the coreclr and the corefx directories in the emulator
    sudo rm -rf "$__ARMRootfsCoreclrPath" "$__ARMRootfsCorefxPath"
}

#Clean the changes made to the environment by the script
function clean_env {
    #Clean the emulator
    clean_emulator

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
        sudo mkdir "$__ARMRootfsMountPath"
    fi

    set +x
    mount_with_checking "" "$__ARMEmulPath/platform/rootfs-t30.ext4" "$__ARMRootfsMountPath"
    mount_with_checking "-t proc" "/proc"    "$__ARMRootfsMountPath/proc"
    mount_with_checking "-o bind" "/dev/"    "$__ARMRootfsMountPath/dev"
    mount_with_checking "-o bind" "/dev/pts" "$__ARMRootfsMountPath/dev/pts"
    mount_with_checking "-t tmpfs" "shm"     "$__ARMRootfsMountPath/run/shm"
    mount_with_checking "-o bind" "/sys"     "$__ARMRootfsMountPath/sys"
    if [ ! -d "$__ARMRootfsMountPath/bindings/tmp" ]; then
        sudo mkdir -p "$__ARMRootfsMountPath/bindings/tmp"
    fi
    mount_with_checking "-o bind" "/mnt"     "$__ARMRootfsMountPath/bindings/tmp"
}

#Cross builds coreclr
function cross_build_coreclr {
#Export the needed environment variables
    (set +x; echo 'Exporting LINUX_ARM_* environment variable')
    source "$__ARMRootfsMountPath"/dotnet/setenv/setenv_incpath.sh "$__ARMRootfsMountPath"

    #Apply the changes needed to build for the emulator rootfs
    (set +x; echo 'Applying cross build patch to suit Linux ARM emulator rootfs')
    git am < "$__ARMRootfsMountPath"/dotnet/setenv/coreclr_cross.patch

    #Apply release optimization patch if needed
    if [[ "$__buildConfig" == "Release" ]]; then
        (set +x; echo 'Applying release optimization patch to build in Release mode')
        git am < "$__ARMRootfsMountPath"/dotnet/setenv/coreclr_release.patch
    fi

    #Cross building for emulator rootfs
    ROOTFS_DIR="$__ARMRootfsMountPath" CPLUS_INCLUDE_PATH=$LINUX_ARM_INCPATH CXXFLAGS=$LINUX_ARM_CXXFLAGS ./build.sh $__buildArch cross $__verboseFlag $__skipMscorlib clang3.5 $__buildConfig -rebuild

    #Reset the code to the upstream version
    (set +x; echo 'Rewinding HEAD to master code')
    git reset --hard HEAD^
    if [[ "$__buildConfig" == "Release" ]]; then
        git reset --hard HEAD^
    fi
}

#Copy the needed files to the emulator to run tests
function copy_to_emulator {

    #Create the coreclr and corefx directories in the emulator
    sudo mkdir -p "$__ARMRootfsCoreclrPath/bin/obj/$__buildDirName"
    sudo mkdir -p "$__ARMRootfsCoreclrPath/bin/Product"
    sudo mkdir "$__ARMRootfsCorefxPath"

    #Copy all coreclr files to the coreclr root in the emulator and set the paths accordingly
    local testRootDirBase=`basename "$__testRootDir"`
    sudo cp -R "$__testRootDir" "$__ARMRootfsCoreclrPath/$testRootDirBase"
    __testRootDirBase="$__ARMEmulCoreclr/$testRootDirBase"

    sudo cp -R "./$__testNativeBinDirBase" "$__ARMRootfsCoreclrPath/$__testNativeBinDirBase"
    __testNativeBinDirBase="$__ARMEmulCoreclr/$__testNativeBinDirBase"

    sudo cp -R "./$__coreClrBinDirBase" "$__ARMRootfsCoreclrPath/$__coreClrBinDirBase"
    if [ ! -z "$__mscorlibDir" ]; then
        sudo cp "$__mscorlibDir/mscorlib.dll" "$__ARMRootfsCoreclrPath/$__coreClrBinDirBase/"
    else
        sudo cp "./$__coreClrBinDirBase/mscorlib.dll" "$__ARMRootfsCoreclrPath/$__coreClrBinDirBase/"
    fi
    __coreClrBinDirBase="$__ARMEmulCoreclr/$__coreClrBinDirBase"
    __mscorlibDirBase="$__coreClrBinDirBase"

    local testDirFileBase=`basename "$__testDirFile"`
    sudo cp "$__testDirFile" "$__ARMRootfsCoreclrPath/$testDirFileBase"
    __testDirFileBase="$__ARMEmulCoreclr/$testDirFileBase"

    sudo cp -R ./tests "$__ARMRootfsCoreclrPath/"
    sudo cp -R ./packages "$__ARMRootfsCoreclrPath/"
    sudo cp -R ./Tools "$__ARMRootfsCoreclrPath/"

    #Copy corefx binary directories to the corefx root in the emulator (first native and then managed)
    local coreFxNativeBinDirBase=`basename "$__coreFxNativeBinDir"`
    sudo cp -R "$__coreFxNativeBinDir" "$__ARMRootfsCorefxPath/$coreFxNativeBinDirBase"
    __coreFxNativeBinDirBase="$__ARMEmulCorefx/$coreFxNativeBinDirBase"

    __coreFxBinDirBase=
    while IFS=';' read -ra coreFxBinDirectories; do
        for currDir in "${coreFxBinDirectories[@]}"; do
            local currDirBase=`basename "$currDir"`
            sudo cp -R "$currDir" "$__ARMRootfsCorefxPath/$currDirBase"

            if [ -z "$__coreFxBinDirBase" ]; then
                __coreFxBinDirBase="$__ARMEmulCorefx/$currDirBase"
            else
                __coreFxBinDirBase="$__coreFxBinDirBase;$__ARMEmulCorefx/$currDirBase"
            fi
        done
    done <<< "$__coreFxBinDir" 
}

#Runs tests in an emulated mode
function run_tests {
    sudo chroot $__ARMRootfsMountPath /bin/bash -x <<EOF
        cd "$__ARMEmulCoreclr"
        ./tests/runtest.sh --testRootDir=$__testRootDirBase \
                           --mscorlibDir=$__mscorlibDirBase \
                           --coreFxNativeBinDir=$__coreFxNativeBinDirBase \
                           --coreFxBinDir="$__coreFxBinDirBase" \
                           --testDirFile=$__testDirFileBase \
                           --testNativeBinDir=$__testNativeBinDirBase \
                           --coreClrBinDir=$__coreClrBinDirBase
EOF
}

#Define script variables
__ARMEmulPath=
__ARMRootfsMountPath=
__buildConfig=
__skipTests=0
__skipMscorlib=
__testRootDir=
__mscorlibDir=
__coreFxNativeBinDir=
__coreFxBinDir=
__testDirFile=
__verboseFlag=
__buildOS="Linux"
__buildArch="arm-softfp"
__buildDirName=
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
    --skipTests)
        __skipTests=1
        ;;
    --skipmscorlib)
        __skipMscorlib="skipmscorlib"
        ;;
    -v|--verbose)
        __verboseFlag="verbose"
        ;;
    --testRootDir=*)
        __testRootDir=${arg#*=}
        ;;
    --mscorlibDir=*)
        __mscorlibDir=${arg#*=}
        ;;
    --coreFxNativeBinDir=*)
        __coreFxNativeBinDir=${arg#*=}
        ;;
    --coreFxBinDir=*)
        __coreFxBinDir=${arg#*=}
        ;;
    --testDirFile=*)
        __testDirFile=${arg#*=}
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
exit_if_path_absent "$__ARMEmulPath/platform/rootfs-t30.ext4" "Path specified in --emulatorPath does not have the rootfs" false

#Check if the optional arguments are present in the case that testing is to be done
if [ $__skipTests == 0 ]; then
    exit_if_empty "$__testRootDir" "Testing requested, but --testRootDir not provided" true
    exit_if_path_absent "$__testRootDir" "Path specified in --testRootDir does not exist" false

    exit_if_empty "$__coreFxNativeBinDir" "Testing requested but --coreFxNativeBinDir not provided" true
    exit_if_path_absent "$__coreFxNativeBinDir" "Path specified in --coreFxNativeBinDir does not exist" false

    exit_if_empty "$__coreFxBinDir" "Testing requested, but --coreFxBinDir not provided" true
    while IFS=';' read -ra coreFxBinDirectories; do
        for currDir in "${coreFxBinDirectories[@]}"; do
            exit_if_path_absent "$currDir" "Path specified in --coreFxBinDir, $currDir does not exist" false
        done
    done <<< "$__coreFxBinDir"

    exit_if_empty "$__testDirFile" "Testing requested, but --testDirFile not provided" true
    exit_if_path_absent "$__testDirFile" "Path specified in --testDirFile does not exist" false

    if [ ! -z "$__skipMscorlib" ]; then
        exit_if_empty "$__mscorlibDir" "Testing and skipmscorlib requested, but --mscorlibDir not provided" true
    fi
    if [ ! -z "$__mscorlibDir" ]; then
        echo '--mscorlibDir provided; will be using this path for running tests and ignoring the generated mscorlib.dll'
        exit_if_path_absent "$__mscorlibDir/mscorlib.dll" "Path specified in --mscorlibDir does not contain mscorlib.dll"
    fi
fi

#Change build configuration to the capitalized form to create build product paths correctly
if [[ "$__buildConfig" == "release" ]]; then
    __buildConfig="Release"
else
    __buildConfig="Debug"
fi
__buildDirName="$__buildOS.$__buildArch.$__buildConfig"

#Define emulator paths
__TempFolder="bindings/tmp/arm32_ci_temp"

if [ ! -d "$__TempFolder" ]; then
    mkdir "$__TempFolder"
fi

__ARMRootfsCoreclrPath="$__ARMRootfsMountPath/$__TempFolder/coreclr"
__ARMRootfsCorefxPath="$__ARMRootfsMountPath/$__TempFolder/corefx"
__ARMEmulCoreclr="/$__TempFolder/coreclr"
__ARMEmulCorefx="/$__TempFolder/corefx"
__testRootDirBase=
__mscorlibDirBase=
__coreFxNativeBinDirBase=
__coreFxBinDirBase=
__testDirFileBase=
__testNativeBinDirBase="bin/obj/$__buildDirName/tests"
__coreClrBinDirBase="bin/Product/$__buildDirName"

set -x
set -e

## Begin cross build
(set +x; echo "Git HEAD @ $__initialGitHead")

#Mount the emulator
(set +x; echo 'Mounting emulator...')
mount_emulator

#Clean the emulator
(set +x; echo 'Cleaning emulator...')
clean_emulator

#Complete the cross build
(set +x; echo 'Building coreclr...')
cross_build_coreclr

#If tests are to be skipped end the script here, else continue
if [ $__skipTests == 1 ]; then
    exit 0
fi

## Tests are going to be performed in an emulated environment

#Copy the needed files to the emulator before entering the emulated environment
(set +x; echo 'Setting up emulator to run tests...')
copy_to_emulator

#Enter the emulated mode and run the tests
(set +x; echo 'Running tests...')
run_tests

#Clean the environment
(set +x; echo 'Cleaning environment...')
clean_env

rm -r "/mnt/arm32_ci_temp"

(set +x; echo 'Build and test complete')
