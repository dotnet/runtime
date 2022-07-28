#!/usr/bin/env bash

#Parse command line arguments
__buildConfig=
for arg in "$@"
do
    case $arg in
    --buildConfig=*)
        __buildConfig="$(echo ${arg#*=} | tr "[:upper:]" "[:lower:]")"
        if [[ "$__buildConfig" != "debug" && "$__buildConfig" != "release" && "$__buildConfig" != "checked" ]]; then
            exit_with_error "--buildConfig can be only Debug or Release" true
        fi
        ;;
    *)
        ;;
    esac
done

#Check if there are any uncommitted changes in the source directory as git adds and removes patches
if [[ -n $(git status -s) ]]; then
   echo 'ERROR: There are some uncommitted changes. To avoid losing these changes commit them and try again.'
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
__buildDirName="$__targetOS.$__buildArch.$__buildConfig"

set -x
set -e

__dockerImage="hseok82/dotnet-buildtools-prereqs:ubuntu-16.04-crossx86-ef0ac75-20175511035548"

# Begin cross build
# We cannot build nuget package yet
__dockerEnvironmentSet="-e ROOTFS_DIR=/crossrootfs/x86"
__currentWorkingDir=`pwd`
__dockerCmd="docker run -i --rm ${__dockerEnvironmentSet} -v $__currentWorkingDir:/opt/code -w /opt/code $__dockerImage"
__buildCmd="./build.sh x86 cross skipnuget $__buildConfig"
$__dockerCmd $__buildCmd

# Begin PAL test
__dockerImage="hseok82/dotnet-buildtools-prereqs:ubuntu1604_x86_test"
__dockerCmd="docker run -i --rm -v $__currentWorkingDir:/opt/code -w /opt/code $__dockerImage"
__palTestCmd="./src/pal/tests/palsuite/runpaltests.sh /opt/code/artifacts/obj/Linux.x86.${__buildConfig} /opt/code/artifacts/paltestout"
$__dockerCmd $__palTestCmd

sudo chown -R $(id -u -n) artifacts/

(set +x; echo 'Build complete')
