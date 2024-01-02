#!/usr/bin/env bash

set -e

opt=$1
case "$opt" in

    libraries)
        # prebuild the repo, so it is ready for development
        ./build.sh libs+clr -rc Release
        # restore libs tests so that the project is ready to be loaded by OmniSharp
        ./build.sh libs.tests -restore
    ;;

    android)
        # prebuild the repo for Mono, so it is ready for development
        ./build.sh mono+libs -os android
        # restore libs tests so that the project is ready to be loaded by OmniSharp
        ./build.sh libs.tests -restore
    ;;

    wasm)
        # prebuild for WASM, so it is ready for wasm development
        make -C src/mono/browser provision-wasm
        export EMSDK_PATH=$PWD/src/mono/browser/emsdk
        ./build.sh mono+libs -os browser -c Release

        # install dotnet-serve for running wasm samples
        ./dotnet.sh tool install dotnet-serve --version 1.10.172 --tool-path ./.dotnet-tools-global
    ;;
esac

# save the commit hash of the currently built assemblies, so developers know which version was built
git rev-parse HEAD > ./artifacts/prebuild.sha
