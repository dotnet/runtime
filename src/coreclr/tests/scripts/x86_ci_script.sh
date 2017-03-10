#!/bin/bash

#Parse command line arguments
__buildConfig=
for arg in "$@"
do
    case $arg in
    --buildConfig=*)
        __buildConfig="$(echo ${arg#*=} | awk '{print tolower($0)}')"
        if [[ "$__buildConfig" != "debug" && "$__buildConfig" != "release" && "$__buildConfig" != "checked" ]]; then
            exit_with_error "--buildConfig can be only Debug or Release" true
        fi
        ;;
    *)
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

#Change build configuration to the capitalized form to create build product paths correctly
if [[ "$__buildConfig" == "release" ]]; then
    __buildConfig="Release"
elif [[ "$__buildConfig" == "checked" ]]; then
    __buildConfig="Checked"
else
    __buildConfig="Debug"
fi
__buildDirName="$__buildOS.$__buildArch.$__buildConfig"

set -x
set -e

__currentWorkingDir=`pwd`
__dockerImage=" microsoft/dotnet-buildtools-prereqs:ubuntu1604_cross_prereqs_v3"
__dockerCmd="sudo docker run --privileged -i --rm -v $__currentWorkingDir:/opt/code -w /opt/code $__dockerImage"

# make rootfs for x86
__buildRootfsCmd="./cross/build-rootfs.sh x86 xenial --skipunmount"
(set +x; echo "Build RootFS for x86 xenial")
$__dockerCmd $__buildRootfsCmd
sudo chown -R $(id -u -n) cross/rootfs/

# Begin cross build
# We cannot build nuget package yet
__buildCmd="./build.sh x86 cross skiptests skipnuget $__buildConfig"
$__dockerCmd $__buildCmd
sudo chown -R $(id -u -n) bin/


(set +x; echo 'Build complete')
