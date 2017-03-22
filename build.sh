#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

# Set OFFLINE environment variable to build offline

set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

# Some things depend on HOME and it may not be set. We should fix those things, but until then, we just patch a value in
if [ -z "$HOME" ] || [ ! -d "$HOME" ]; then
    export HOME=$DIR/artifacts/home

    [ ! -d "$HOME" ] || rm -Rf $HOME
    mkdir -p $HOME
fi

while [[ $# > 0 ]]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        --docker)
            export BUILD_IN_DOCKER=1
            export DOCKER_IMAGENAME=$2
            shift
            ;;
        *)
            args+=( $1 )
            ;;
    esac
    shift
done

# $args array may have empty elements in it.
# The easiest way to remove them is to cast to string and back to array.
temp="${args[@]}"
args=($temp)

dockerbuild()
{
    BUILD_COMMAND=/opt/code/build_projects/dotnet-host-build/build.sh $DIR/scripts/dockerrun.sh --non-interactive "$@"
}

# Check if we need to build in docker
if [ ! -z "$BUILD_IN_DOCKER" ]; then
    dockerbuild "${args[@]}"
else
    $DIR/build_projects/dotnet-host-build/build.sh "${args[@]}"
fi
