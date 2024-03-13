#!/usr/bin/env bash

set -e

function wasm_common() {
    # prebuild for WASM, so it is ready for wasm development
    make -C src/mono/browser provision-wasm
    export EMSDK_PATH=$PWD/src/mono/browser/emsdk
    case "$1" in
    wasm)
        # Put your common commands for wasm here
        echo "[DEBUG] restoring for wasm..."
        ./build.sh mono+libs -os browser -c Release --restore
        echo "[DEBUG] building for wasm..."
        ./build.sh mono+libs -os browser -c Release --build
        echo "[DEBUG] building succeeded"
        ;;
    wasm-multithreaded)
        # Put your common commands for wasm-multithread here
        echo "[DEBUG] restoring for wasm-multithreaded..."
        ./build.sh mono+libs -os browser -c Release /p:WasmEnableThreads=true --restore
        echo "[DEBUG] building for wasm-multithreaded..."
        ./build.sh mono+libs -os browser -c Release /p:WasmEnableThreads=true --build
        echo "[DEBUG] building succeeded"
        ;;
    *)
        # install dotnet-serve for running wasm samples
    ./dotnet.sh tool install dotnet-serve --version 1.10.172 --tool-path ./.dotnet-tools-global
    ;;
    esac
}

# Start background jobs that run df -i, free -h, and df -h every second
while true; do echo -e "$(date)\n$(df -i)"; sleep 5; done >> inodes.txt &
inode_pid=$!

while true; do echo -e "$(date)\n$(free -h)"; sleep 5; done >> ram.txt &
ram_pid=$!

while true; do echo -e "$(date)\n$(df -h)"; sleep 5; done >> disk.txt &
disk_pid=$!

# Set traps to kill the background jobs when the script exits
trap "kill $inode_pid $ram_pid $disk_pid" EXIT

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
