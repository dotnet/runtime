#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

INTERACTIVE="-i"

while [[ $# > 0 ]]; do
    key=$1

    case $key in
        --non-interactive)
            INTERACTIVE=
            ;;
        -i|--image)
            DOCKER_IMAGENAME=$2
            shift
            ;;
        -d|--dockerfile)
            DOCKERFILE=$2
            shift
            ;;
        -h|-?|--help)
            echo "Usage: $0 [-d|--dockerfile <Dockerfile>] [-i|--image <ImageName>] <Command>"
            echo ""
            echo "Options:"
            echo "  <Dockerfile>    The path to the Dockerfile to use to create the build container"
            echo "  <ImageName>     The name of an existing Dockerfile folder under scripts/docker to use as the Dockerfile"
            echo "  <Command>  The command to run once inside the container (/opt/code is mapped to the repo root; defaults to nothing, which runs the default shell)"
            exit 0
            ;;
        *)
            break # the first non-switch we get ends parsing
            ;;
    esac

    shift
done

if [ -z "$DOCKERFILE" ]; then
    if [ -z "$DOCKER_IMAGENAME" ]; then
        if [ "$(uname)" == "Darwin" ]; then
            echo "Defaulting to 'ubuntu' image for Darwin"
            export DOCKERFILE=scripts/docker/ubuntu
        elif [ "$(cat /etc/*-release | grep -cim1 ubuntu)" -eq 1 ]; then
            echo "Detected current OS as Ubuntu, determining ubuntu version to use..."
            if [ "$(cat /etc/*-release | grep -cim1 16.04)" -eq 1 ]; then
                echo "using 'ubuntu.16.04' image"
                export DOCKERFILE=scripts/docker/ubuntu.16.04
            else
                echo "using 'ubuntu' image"
                export DOCKERFILE=scripts/docker/ubuntu
            fi
        elif [ "$(cat /etc/*-release | grep -cim1 centos)" -eq 1 ]; then
            echo "Detected current OS as CentOS, using 'centos' image"
            export DOCKERFILE=scripts/docker/centos
        elif [ "$(cat /etc/*-release | grep -cim1 debian)" -eq 1 ]; then
            echo "Detected current OS as Debian, using 'debian' image"
            export DOCKERFILE=scripts/docker/debian
        else
            echo "Unknown Linux Distro. Using 'ubuntu' image"
            export DOCKERFILE=scripts/docker/ubuntu
        fi
    else
        echo "Using requested image: $DOCKER_IMAGENAME"
        export DOCKERFILE="scripts/docker/$DOCKER_IMAGENAME"
    fi
fi

[ -z "$DOTNET_BUILD_CONTAINER_TAG" ] && DOTNET_BUILD_CONTAINER_TAG="dotnet-coresetup-build"
[ -z "$DOTNET_BUILD_CONTAINER_NAME" ] && DOTNET_BUILD_CONTAINER_NAME="dotnet-coresetup-build-container"
[ -z "$DOCKER_HOST_SHARE_DIR" ] && DOCKER_HOST_SHARE_DIR="$(pwd)"

# Make container names CI-specific if we're running in CI
#  Jenkins
[ ! -z "$BUILD_TAG" ] && DOTNET_BUILD_CONTAINER_NAME="$BUILD_TAG"
#  VSO
[ ! -z "$BUILD_BUILDID" ] && DOTNET_BUILD_CONTAINER_NAME="${BUILD_BUILDID}-${BUILD_BUILDNUMBER}"

# Build the docker container (will be fast if it is already built)
echo "Building Docker Container using Dockerfile: $DOCKERFILE"
docker build --build-arg USER_ID=$(id -u) -t $DOTNET_BUILD_CONTAINER_TAG $DOCKERFILE

# Run the build in the container
echo "Launching build in Docker Container"
echo "Running command: $BUILD_COMMAND"
echo "Using code from: $DOCKER_HOST_SHARE_DIR"
[ -z "$INTERACTIVE" ] || echo "Running Interactive"

docker run $INTERACTIVE -t --rm --sig-proxy=true \
    --name $DOTNET_BUILD_CONTAINER_NAME \
    -v $DOCKER_HOST_SHARE_DIR:/opt/code \
    -e CHANNEL \
    -e CONNECTION_STRING \
    -e REPO_ID \
    -e REPO_USER \
    -e REPO_PASS \
    -e REPO_SERVER \
    -e DOTNET_BUILD_SKIP_CROSSGEN \
    -e PUBLISH_TO_AZURE_BLOB \
    -e DOCKER_HUB_REPO \
    -e DOCKER_HUB_TRIGGER_TOKEN \
    -e NUGET_FEED_URL \
    -e NUGET_API_KEY \
    -e GITHUB_PASSWORD \
    $DOTNET_BUILD_CONTAINER_TAG \
    $BUILD_COMMAND "$@"

# Remove the container if it stuck around, ignore failure here
set +e
docker rm -f $DOTNET_BUILD_CONTAINER_NAME

# This won't be hit if a failure happened above, but forces ignoring the rm failure, which we don't care about
exit 0