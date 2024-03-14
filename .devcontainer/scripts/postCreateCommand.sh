#!/usr/bin/env bash

set -e

function wasm_common() {
    # moving this to post-create for wasm till
    # https://github.com/orgs/community/discussions/57767 gets resolved
    # prebuild for WASM, so it is ready for wasm development
    export EMSDK_PATH=$PWD/src/mono/browser/emsdk
    case "$1" in
    wasm)
        # Put your common commands for wasm here
        ./build.sh mono+libs -os browser -c Release
        ;;
    wasm-multithreaded)
        # Put your common commands for wasm-multithread here        
        ./build.sh mono+libs -os browser -c Release /p:WasmEnableThreads=tru
        ;;
    *)
    # install dotnet-serve for running wasm samples
    ./dotnet.sh tool install dotnet-serve --version 1.10.172 --tool-path ./.dotnet-tools-global
    ;;
    esac
}

opt=$1
case "$opt" in

    android)
        # Create the Android emulator.
        ${ANDROID_SDK_ROOT}/cmdline-tools/cmdline-tools/bin/avdmanager -s create avd --name ${EMULATOR_NAME_X64} --package "system-images;android-${SDK_API_LEVEL};default;x86_64"
    ;;

    wasm)
        wasm_common $opt
    ;;

    wasm-multithreaded)
        wasm_common $opt
    ;;
esac

# reset the repo to the commit hash that was used to build the prebuilt Codespace
git reset --hard $(cat ./artifacts/prebuild.sha)