#!/usr/bin/env bash
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#

set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

cd "$DIR/.."

while [[ $# > 0 ]]; do
    key=$1

    case $key in
        -t|--tag)
            DOCKER_TAG=$2
            shift
            ;;
        -d|--dockerfile)
            DOCKERFILE=$2
            shift
            ;;
        -h|-?|--help)
            echo "Usage: $0 [-d|--dockerfile <Dockerfile>] [-t|--tag <Tag>] <Command>"
            echo ""
            echo "Options:"
            echo "  <Dockerfile>    The path to the folder that contains a Dockerfile to use to create the build container"
            echo "  <Tag>           The name of docker image tag"
            exit 0
            ;;
        *)
            break # the first non-switch we get ends parsing
            ;;
    esac

    shift
done

# Executes a command and retries if it fails.
# NOTE: This function is the exact copy from init-docker.sh.
# Reason for not invoking init.docker.sh directly is since that script 
# also performs cleanup, which we do not want in this case.
execute() {
    local count=0
    local retries=5
    local waitFactor=6
    until "$@"; do
        local exit=$?
        count=$(( $count + 1 ))
        if [ $count -lt $retries ]; then
            local wait=$(( waitFactor ** (( count - 1 )) ))
            echo "Retry $count/$retries exited $exit, retrying in $wait seconds..."
            sleep $wait
        else    
            say_err "Retry $count/$retries exited $exit, no more retries left."
            return $exit
        fi
    done

    return 0
}

# Build the docker container (will be fast if it is already built)
echo "Building Docker Container using Dockerfile: $DOCKERFILE"

# Get the name of Docker image.
image=$(grep -i "^FROM " "$DOCKERFILE/Dockerfile" | awk '{ print $2 }')

# Explicitly pull the base image with retry logic. 
# This eliminates intermittent failures during docker build caused by failing to retrieve the base image.
if [ ! -z "$image" ]; then
    echo "Pulling Docker image $image"
    execute docker pull $image
fi

docker build --build-arg USER_ID=$(id -u) -t $DOCKER_TAG $DOCKERFILE
