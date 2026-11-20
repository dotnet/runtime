#!/usr/bin/env bash

set -e

function wasm_common() {
    case "$1" in
    wasm)
        # Put your common commands for wasm here
        # NativeAOT cDAC is not supported on browser-wasm
        ./build.sh -os browser -c Release /p:SupportsNativeAotComponents=false
        ;;
    wasm-multithreaded)
        # Put your common commands for wasm-multithread here
        # Multithreading is not supported on CoreCLR yet
        echo "Warning: WASM multithreading is not supported on CoreCLR yet, skipping prebuild." >&2
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
        # prebuild the repo so it is ready for development
        ./build.sh libs+clr.runtime+clr.alljits+clr.corelib+clr.nativecorelib+clr.tools+clr.packages -os android
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

# save the commit hash used for the devcontainer setup
mkdir -p ./artifacts
git rev-parse HEAD > ./artifacts/prebuild.sha
