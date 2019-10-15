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
            echo "  <Dockerfile>    The path to the folder that contains a Dockerfile to use to create the build container"
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
        else
            if [ -e /etc/os-release ]; then
               source /etc/os-release

               if [ -d "scripts/docker/$ID.$VERSION_ID" ]; then
                   echo "Using '$ID.$VERSION_ID' image"
                   export DOCKERFILE="scripts/docker/$ID.$VERSION_ID"
               else
                   echo "Unknown Linux Distro. Using 'ubuntu.14.04' image"
                   export DOCKERFILE="scripts/docker/ubuntu.14.04"
               fi
            else
                echo "Unknown Linux Distro. Using 'ubuntu.14.04' image"
                export DOCKERFILE="scripts/docker/ubuntu.14.04"
            fi
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

#Build the docker image
"$DIR/dockerbuild.sh" -t $DOTNET_BUILD_CONTAINER_TAG -d $DOCKERFILE

# Run the build in the container
echo "Launching build in Docker Container"
echo "Running command: $BUILD_COMMAND"
echo "Using code from: $DOCKER_HOST_SHARE_DIR"
[ -z "$INTERACTIVE" ] || echo "Running Interactive"

docker run $INTERACTIVE -t --rm --sig-proxy=true \
    --name $DOTNET_BUILD_CONTAINER_NAME \
    -v $DOCKER_HOST_SHARE_DIR:/opt/code \
    -e NUGET_FEED_URL \
    -e NUGET_SYMBOLS_FEED_URL \
    -e NUGET_API_KEY \
    -e GITHUB_PASSWORD \
    $DOTNET_BUILD_CONTAINER_TAG \
    $BUILD_COMMAND "$@"

# Remove the container if it stuck around, ignore failure here
set +e
docker rm -f $DOTNET_BUILD_CONTAINER_NAME

# This won't be hit if a failure happened above, but forces ignoring the rm failure, which we don't care about
exit 0
