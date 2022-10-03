#!/usr/bin/env bash

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
    echo '    --testRootDir=~/Downloads/windows.x64.Release'
    echo '    --mscorlibDir=~/clr/bin/bin/coreclr/Linux.armel.Release'
    echo '    --coreFxNativeBinDir=~/cfx/bin/Linux.armel.Release'
    echo '    --coreFxBinDir="~/cfx/bin/Linux.AnyCPU.Release;~/cfx/bin/Unix.AnyCPU.Release;~/cfx/bin/AnyOS.AnyCPU.Release"'
    echo '    --testDirFile=~/clr/tests/testsRunningInsideARM.txt'
    echo ''
    echo 'Required Arguments:'
    echo '    --emulatorPath=<path>              : Path of the emulator folder (without ending /)'
    echo '                                         <path>/platform/rootfs-t30.ext4 should exist'
    echo '    --mountPath=<path>                 : The desired path for mounting the emulator rootfs (without ending /)'
    echo '                                         This path is created if not already present'
    echo '    --buildConfig=<config>             : The value of config should be either Debug, Checked or Release'
    echo '                                         Any other value is not accepted'
    echo 'Optional Arguments:'
    echo '    --mode=<mode>                      : docker or emulator (default)'
    echo '    --arm                              : Build using hard ABI'
    echo '    --armel                            : Build using softfp ABI (default)'
    echo '    --linuxCodeName=<name>             : Code name for Linux: For arm, trusty (default) and xenial. For armel, tizen'
    echo '    --skipRootFS                       : Skip building rootfs'
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
    if [[ "$printUsage" == "true" ]]; then
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

    sudo rm -rf "/mnt/arm32_ci_temp"
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

    if [[ "$__ciMode" == "emulator" ]]; then
        clean_env
    fi
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
    #Check if the mount path exists and create if necessary
    if [ ! -d "$__ARMRootfsMountPath" ]; then
        sudo mkdir "$__ARMRootfsMountPath"
    fi

    if [ ! -d "$__ARMEmulRootfs" ]; then
        sudo mkdir "$__ARMEmulRootfs"
    fi

    if [ ! -f "$__ARMEmulRootfs/arm-emulator-rootfs.tar" ]; then
        if mountpoint -q -- "$__ARMRootfsMountPath"; then
            sudo umount -l $__ARMRootfsMountPath
        fi
        mount_with_checking "" "$__ARMEmulPath/platform/rootfs-t30.ext4" "$__ARMRootfsMountPath"

        cd $__ARMRootfsMountPath
        sudo tar -cf "$__ARMEmulRootfs/arm-emulator-rootfs.tar" *
        cd -
    fi

    sudo tar -xf "$__ARMEmulRootfs/arm-emulator-rootfs.tar" -C "$__ARMEmulRootfs"

    mount_with_checking "-t proc" "/proc"    "$__ARMEmulRootfs/proc"
    mount_with_checking "-o bind" "/dev/"    "$__ARMEmulRootfs/dev"
    mount_with_checking "-o bind" "/dev/pts" "$__ARMEmulRootfs/dev/pts"
    mount_with_checking "-t tmpfs" "shm"     "$__ARMEmulRootfs/run/shm"
    mount_with_checking "-o bind" "/sys"     "$__ARMEmulRootfs/sys"
    if [ ! -d "$__ARMEmulRootfs/bindings/tmp" ]; then
        sudo mkdir -p "$__ARMEmulRootfs/bindings/tmp"
    fi
    mount_with_checking "-o bind" "/mnt"     "$__ARMEmulRootfs/bindings/tmp"

    if [ ! -d "$__ARMEmulRootfs/$__TempFolder" ]; then
        sudo mkdir "$__ARMEmulRootfs/$__TempFolder"
    fi
}

#Cross builds coreclr
function cross_build_coreclr {
#Export the needed environment variables
    (set +x; echo 'Exporting LINUX_ARM_* environment variable')
    source "$__ARMEmulRootfs"/dotnet/setenv/setenv_incpath.sh "$__ARMEmulRootfs"

    #Apply release optimization patch if needed
    if [[ "$__buildConfig" == "Release" ]]; then
        (set +x; echo 'Applying release optimization patch to build in Release mode')
        git am < "$__ARMEmulRootfs"/dotnet/setenv/coreclr_release.patch
    fi

    #Cross building for emulator rootfs
    ROOTFS_DIR="$__ARMEmulRootfs" CPLUS_INCLUDE_PATH=$LINUX_ARM_INCPATH CXXFLAGS=$LINUX_ARM_CXXFLAGS ./build.sh $__buildArch cross $__verboseFlag $__skipMscorlib clang3.5 $__buildConfig

    #Reset the code to the upstream version
    (set +x; echo 'Rewinding HEAD to main code')
    if [[ "$__buildConfig" == "Release" ]]; then
        git reset --hard HEAD^
    fi
}

#Cross builds coreclr using Docker
function cross_build_coreclr_with_docker {
    __currentWorkingDirectory=`pwd`

    # Check build configuration and choose Docker image
    __dockerEnvironmentVariables=""
    if [[ "$__buildArch" == "arm" ]]; then
        # TODO: For arm, we are going to embed RootFS inside Docker image.
        case $__linuxCodeName in
        trusty)
            __dockerImage=" mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-14.04-cross-0cd4667-20172211042239"
            __skipRootFS=1
            __dockerEnvironmentVariables+=" -e ROOTFS_DIR=/crossrootfs/arm"
            __runtimeOS="ubuntu.14.04"
        ;;
        xenial)
            __dockerImage=" mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-ef0ac75-20175511035548"
            __skipRootFS=1
            __dockerEnvironmentVariables+=" -e ROOTFS_DIR=/crossrootfs/arm"
            __runtimeOS="ubuntu.16.04"
        ;;
        *)
            exit_with_error "ERROR: $__linuxCodeName is not a supported linux name for $__buildArch" false
        ;;
        esac
    elif [[ "$__buildArch" == "armel" ]]; then
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
    __dockerCmd="sudo docker run ${__dockerEnvironmentVariables} --privileged -i --rm -v $__currentWorkingDirectory:/opt/code -w /opt/code $__dockerImage"

    if [[ "$__skipRootFS" == 0 ]]; then
        # Build rootfs
        __buildRootfsCmd="$__RepoRootDir/eng/common/cross/build-rootfs.sh $__buildArch $__linuxCodeName --skipunmount"

        (set +x; echo "Build RootFS for $__buildArch $__linuxCodeName")
        $__dockerCmd $__buildRootfsCmd
        sudo chown -R $(id -u -n) $__RepoRootDir/eng/common/cross/rootfs
    fi

    __extraArgs=""
    if [[ "$__buildArch" == "armel" && "$__linuxCodeName" == "tizen" ]]; then
        __extraArgs="cmakeargs -DFEATURE_NGEN_RELOCS_OPTIMIZATIONS=true cmakeargs -DFEATURE_GDBJIT=true cmakeargs -DFEATURE_PREJIT=true -PortableBuild=false"
    fi

    # Cross building coreclr with rootfs in Docker
    (set +x; echo "Start cross build coreclr for $__buildArch $__linuxCodeName")
    __buildCmd="./build.sh $__buildArch cross $__verboseFlag $__skipMscorlib $__buildConfig $__extraArgs"
    $__dockerCmd $__buildCmd
    sudo chown -R $(id -u -n) ./bin
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
    __coreClrBinDirBase="$__ARMEmulCoreclr/$__coreClrBinDirBase"
    __mscorlibDirBase="$__coreClrBinDirBase"

    local testDirFileBase=`basename "$__testDirFile"`
    sudo cp "$__testDirFile" "$__ARMRootfsCoreclrPath/$testDirFileBase"
    __testDirFileBase="$__ARMEmulCoreclr/$testDirFileBase"

    sudo cp -R ./tests "$__ARMRootfsCoreclrPath/"
    sudo cp -R ./.packages "$__ARMRootfsCoreclrPath/"
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
    sudo chroot $__ARMEmulRootfs /bin/bash -x <<EOF
        cd "$__ARMEmulCoreclr"
        ./bringup_runtest.sh --sequential\
                           --testRootDir=$__testRootDirBase \
                           --mscorlibDir=$__mscorlibDirBase \
                           --coreFxNativeBinDir=$__coreFxNativeBinDirBase \
                           --coreFxBinDir="$__coreFxBinDirBase" \
                           --testDirFile=$__testDirFileBase \
                           --testNativeBinDir=$__testNativeBinDirBase \
                           --coreClrBinDir=$__coreClrBinDirBase
EOF
}

function run_tests_using_docker {
    __currentWorkingDirectory=`pwd`

    # Configure docker
    __dockerEnvironmentVariables=""
    if [[ "$__buildArch" == "arm" ]]; then
        case $__linuxCodeName in
        trusty)
            __dockerImage=" mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu1404_cross_prereqs_v3"
            __skipRootFS=1
            __dockerEnvironmentVariables=" -e ROOTFS_DIR=/crossrootfs/arm"
        ;;
        xenial)
            __dockerImage=" mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu1604_cross_prereqs_v3"
            __skipRootFS=1
            __dockerEnvironmentVariables=" -e ROOTFS_DIR=/crossrootfs/arm"
        ;;
        *)
            exit_with_error "ERROR: $__linuxCodeName is not a supported linux name for $__buildArch" false
        ;;
        esac
    elif [[ "$__buildArch" == "armel" ]]; then
        case $__linuxCodeName in
        tizen)
            __dockerImage=" tizendotnet/dotnet-buildtools-prereqs:ubuntu-16.04-cross-e435274-20180426002255-tizen-rootfs-5.0m1"
            __skipRootFS=1
            __dockerEnvironmentVariables=" -e ROOTFS_DIR=/crossrootfs/armel.tizen.test"
        ;;
        *)
            exit_with_error "ERROR: $__linuxCodeName is not a supported linux name for $__buildArch" false
        ;;
        esac
    else
        exit_with_error "ERROR: unknown buildArch $__buildArch" false
    fi
    __dockerCmd="sudo docker run ${__dockerEnvironmentVariables} --privileged -i --rm -v $__currentWorkingDirectory:/opt/code -w /opt/code $__dockerImage"
    __testCmd="./tests/scripts/arm32_ci_test.sh --abi=${__buildArch} --buildConfig=${__buildConfig}"

    $__dockerCmd $__testCmd
}

__RepoRootDir=./../..

#Define script variables
__ciMode="emulator"
__ARMEmulRootfs=/mnt/arm-emulator-rootfs
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
__targetOS="Linux"
__buildArch="armel"
__linuxCodeName="tizen"
__skipRootFS=0
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
        __buildConfig="$(echo ${arg#*=} | tr "[:upper:]" "[:lower:]")"
        if [[ "$__buildConfig" != "debug" && "$__buildConfig" != "release" && "$__buildConfig" != "checked" ]]; then
            exit_with_error "--buildConfig can be Debug, Checked or Release" true
        fi
        ;;
    --mode=*)
        __ciMode=${arg#*=}
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
    --arm)
        __buildArch="arm"
        ;;
    --armel)
        __buildArch="armel"
        __linuxCodeName="tizen"
        ;;
    --linuxCodeName=*)
        __linuxCodeName=${arg#*=}
        ;;
    --skipRootFS)
        __skipRootFS=1
        ;;
    -h|--help)
        usage
        ;;
    *)
        exit_with_error "$arg not a recognized argument" true
        ;;
    esac
done

#Check if there are any uncommitted changes in the source directory as git adds and removes patches
if [[ -n $(git status -s)  ]]; then
   echo 'ERROR: There are some uncommitted changes. To avoid losing these changes commit them and try again.'
   echo ''
   git status
   exit 1
fi

exit_if_empty "$__buildConfig" "--buildConfig is a mandatory argument, not provided" true
if [[ "$__ciMode" == "emulator" ]]; then
    #Check if the compulsory arguments have been presented to the script and if the input paths exist
    exit_if_empty "$__ARMEmulPath" "--emulatorPath is a mandatory argument, not provided" true
    exit_if_empty "$__ARMRootfsMountPath" "--mountPath is a mandatory argument, not provided" true
    exit_if_path_absent "$__ARMEmulPath/platform/rootfs-t30.ext4" "Path specified in --emulatorPath does not have the rootfs" false
    # Test is not available in emulator mode.
    __skipTests=1
fi

__coreFxBinDir="./bin/CoreFxBinDir" # TODO-cleanup: Just for testing....
#Check if the optional arguments are present in the case that testing is to be done
if [[ "$__skipTests" == 0 ]]; then
    exit_if_empty "$__testRootDir" "Testing requested, but --testRootDir not provided" true
    exit_if_path_absent "$__testRootDir" "Path specified in --testRootDir does not exist" false

    exit_if_empty "$__coreFxBinDir" "Testing requested, but --coreFxBinDir not provided" true
    exit_if_path_absent "$__coreFxBinDir" "Path specified in --coreFxBinDir does not exist" false

    exit_if_empty "$__testDirFile" "Testing requested, but --testDirFile not provided" true
    exit_if_path_absent "$__testDirFile" "Path specified in --testDirFile does not exist" false

    if [[ -n "$__skipMscorlib" ]]; then
        exit_if_empty "$__mscorlibDir" "Testing and skipmscorlib requested, but --mscorlibDir not provided" true
    fi
    if [[ -n "$__mscorlibDir" ]]; then
        echo '--mscorlibDir provided; will be using this path for running tests and ignoring the generated mscorlib.dll'
        exit_if_path_absent "$__mscorlibDir/mscorlib.dll" "Path specified in --mscorlibDir does not contain mscorlib.dll"
    fi
fi

#Change build configuration to the capitalized form to create build product paths correctly
if [[ "$__buildConfig" == "release" ]]; then
    __buildConfig="Release"
elif [[ "$__buildConfig" == "checked" ]]; then
    __buildConfig="Checked"
else
    __buildConfig="Debug"
fi
__buildDirName="$__targetOS.$__buildArch.$__buildConfig"

#Define emulator paths
__TempFolder="bindings/tmp/arm32_ci_temp"
__ARMRootfsCoreclrPath="$__ARMEmulRootfs/$__TempFolder/coreclr"
__ARMRootfsCorefxPath="$__ARMEmulRootfs/$__TempFolder/corefx"
__ARMEmulCoreclr="/$__TempFolder/coreclr"
__ARMEmulCorefx="/$__TempFolder/corefx"
__testRootDirBase=
__mscorlibDirBase=
__coreFxNativeBinDirBase=
__coreFxBinDirBase=
__testDirFileBase=
__testNativeBinDirBase="bin/obj/$__buildDirName/tests"
__coreClrBinDirBase="bin/bin/coreclr/$__buildDirName"

set -x
set -e

## Begin cross build
(set +x; echo "Git HEAD @ $__initialGitHead")

if [[ "$__ciMode" == "docker" ]]; then
    # Complete the cross build using Docker
    (set +x; echo 'Building coreclr...')
    cross_build_coreclr_with_docker
else
    #Mount the emulator
    (set +x; echo 'Mounting emulator...')
    mount_emulator

    #Clean the emulator
    (set +x; echo 'Cleaning emulator...')
    clean_emulator

    #Complete the cross build
    (set +x; echo 'Building coreclr...')
    cross_build_coreclr
fi

#If tests are to be skipped end the script here, else continue
if [[ "$__skipTests" == 1 ]]; then
    exit 0
fi

__unittestResult=0
## Begin CoreCLR test
if [[ "$__ciMode" == "docker" ]]; then
    run_tests_using_docker
    __unittestResult=$?
else
    ## Tests are going to be performed in an emulated environment

    #Copy the needed files to the emulator before entering the emulated environment
    (set +x; echo 'Setting up emulator to run tests...')
    copy_to_emulator

    #Enter the emulated mode and run the tests
    (set +x; echo 'Running tests...')
    run_tests
    __unittestResult=$?

    #Clean the environment
    (set +x; echo 'Cleaning environment...')
    clean_env
fi

(set +x; echo 'Build and test complete')
exit $__unittestResult
