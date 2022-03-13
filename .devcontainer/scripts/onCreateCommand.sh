#!/usr/bin/env bash

set -e

# prebuild the repo, so it is ready for development
./build.sh libs+clr -rc Release
# restore libs tests so that the project is ready to be loaded by OmniSharp
./build.sh libs.tests -restore

# prebuild for WASM, so it is ready for wasm development
make -C src/mono/wasm provision-wasm
export EMSDK_PATH=$PWD/src/mono/wasm/emsdk
./build.sh mono+libs -os Browser -c release

# install dotnet-serve for running wasm samples
./dotnet.sh tool install dotnet-serve --tool-path ./.dotnet-tools-global

# save the commit hash of the currently built assemblies, so developers know which version was built
git rev-parse HEAD > ./artifacts/prebuild.sha
