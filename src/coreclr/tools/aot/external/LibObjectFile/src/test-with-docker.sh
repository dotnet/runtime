#!/bin/bash

export TEST_TAG_NAME=libobjectfile-tests

# To rebuild the image, simply delete the tag:
# docker rmi $TEST_TAG_NAME

if [[ "$(docker images -q ${TEST_TAG_NAME}:latest 2> /dev/null)" == "" ]]; then
    docker build -t ${TEST_TAG_NAME} .
fi

# Run unit tests in Docker - See Dockerfile
docker run -v `pwd`:/src --rm -it ${TEST_TAG_NAME}

