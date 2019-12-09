#!/usr/bin/env bash
#
# Docker builds libraries and produces a dotnet sdk docker image
# that contains the current bits in its shared framework folder.

cd $(dirname $0)

[ -z $DOCKER_IMAGE_TAG ] && IMAGE_TAG=dotnet-sdk-libs-current
[ -z $CONFIGURATION ] && CONFIGURATION=Release

REPO_ROOT_DIR=$(git rev-parse --show-toplevel)

docker build --tag $(IMAGE_TAG) \
             --build-arg CONFIGURATION=$(CONFIGURATION) \
             --file libraries-sdk.linux.Dockerfile \
             $REPO_ROOT_DIR
