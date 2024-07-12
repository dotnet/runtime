#!/usr/bin/env bash

set -e

function wasm_common() {
    case "$1" in
    wasm)
        # Put your common commands for wasm here
        ./build.sh mono+libs -os browser -c Release
        ;;
    wasm-multithreaded)
        # Put your common commands for wasm-multithread here
        ./build.sh mono+libs -os browser -c Release /p:WasmEnableThreads=true
        ;;
    *)
        # install dotnet-serve for running wasm samples
    ./dotnet.sh tool install dotnet-serve --version 1.10.172 --tool-path ./.dotnet-tools-global
    ;;
    esac
}

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
        wasm_common $opt
    ;;

    wasm-multithreaded)
        wasm_common $opt
    ;;
esac

# save the commit hash of the currently built assemblies, so developers know which version was built
git rev-parse HEAD > ./artifacts/prebuild.sha