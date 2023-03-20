#!/usr/bin/env bash

set -e

# bump the dotnet sdk version to get msbuild bugfix: https://github.com/dotnet/msbuild/issues/8531
# TODO: remove once we're on a newer sdk in global.json
sed -i 's/8.0.100-preview.1.23115.2/8.0.100-preview.3.23159.20/g' global.json || true

opt=$1
case "$opt" in

    libraries)
        # prebuild the repo, so it is ready for development
        ./build.sh libs+clr -rc Release
        # restore libs tests so that the project is ready to be loaded by OmniSharp
        ./build.sh libs.tests -restore
    ;;

    wasm)
        # prebuild for WASM, so it is ready for wasm development
        make -C src/mono/wasm provision-wasm
        export EMSDK_PATH=$PWD/src/mono/wasm/emsdk
        ./build.sh mono+libs -os browser -c Release

        # install dotnet-serve for running wasm samples
        ./dotnet.sh tool install dotnet-serve --tool-path ./.dotnet-tools-global
    ;;
esac

# save the commit hash of the currently built assemblies, so developers know which version was built
git rev-parse HEAD > ./artifacts/prebuild.sha
