#!/bin/bash

function exit_with_error {
    set +x

    local errorMessage="$1"

    echo "ERROR: $errorMessage"
    exit 1
}

#Exit if input string is empty
function exit_if_empty {
    local inputString="$1"
    local errorMessage="$2"

    if [ -z "$inputString" ]; then
        exit_with_error "$errorMessage"
    fi
}

# Cross builds corefx using Docker image
function cross_build_native_with_docker {
    __currentWorkingDirectory=`pwd`

    # choose Docker image
    __dockerImage=" hseok82/dotnet-buildtools-prereqs:ubuntu-16.04-crossx86-ef0ac75-20175511035548"
    __dockerEnvironmentVariables="-e ROOTFS_DIR=/crossrootfs/x86"
    __runtimeOS="ubuntu.16.04"
    
    __dockerCmd="docker run ${__dockerEnvironmentVariables} -i --rm -v $__currentWorkingDirectory:/opt/code -w /opt/code $__dockerImage"

    # Cross building corefx with rootfs in Docker
    __buildNativeCmd="./build-native.sh -buildArch=x86 -$__buildConfig -- cross"
    
    $__dockerCmd $__buildNativeCmd
    sudo chown -R $(id -u -n) ./bin
}

__buildConfig=
for arg in "$@"
do
    case $arg in
        --buildConfig=*)
            __buildConfig="$(echo ${arg#*=} | awk '{print tolower($0)}')"
            if [[ "$__buildConfig" != "debug" && "$__buildConfig" != "release" ]]; then
                exit_with_error "--buildConfig can be only Debug or Release" true
            fi
            ;;
        *)
            ;;
    esac
done

set -x
set -e

exit_if_empty "$__buildConfig" "--buildConfig is a mandatory argument, not provided"

#Complete the cross build
(set +x; echo 'Building corefx...')
cross_build_native_with_docker

(set +x; echo 'Build complete')
