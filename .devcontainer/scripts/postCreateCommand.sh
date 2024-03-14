#!/usr/bin/env bash

set -e

function wasm_common() {
    # prebuild for WASM, so it is ready for wasm development
    echo "[$(date)] provisioning..." >> build.txt
    make -C src/mono/browser provision-wasm
    echo "[$(date)] export emsdk path..." >> build.txt
    export EMSDK_PATH=$PWD/src/mono/browser/emsdk
    case "$1" in
    wasm)
        # Put your common commands for wasm here
        echo "[$(date)] cleaning the packages cache..." >> build.txt
        sudo apt-get clean
        echo "[$(date)] restoring for wasm mono subset..." >> build.txt
        ./build.sh mono -os browser -c Release --restore
        echo "[$(date)] cleaning cache after restore..." >> build.txt
        dotnet nuget locals all --clear
        echo "[$(date)] restoring for wasm libs subset..." >> build.txt
        ./build.sh libs -os browser -c Release --restore
        echo "[$(date)] building for wasm..." >> build.txt
        ./build.sh mono+libs -os browser -c Release --build
        echo "[$(date)] building succeeded" >> build.txt
        ;;
    wasm-multithreaded)
        # Put your common commands for wasm-multithread here
        echo "[$(date)] restoring for wasm-multithreaded..." >> build.txt
        ./build.sh mono+libs -os browser -c Release /p:WasmEnableThreads=true --restore
        echo "[$(date)] building for wasm-multithreaded..." >> build.txt
        ./build.sh mono+libs -os browser -c Release /p:WasmEnableThreads=true --build
        echo "[$(date)] building succeeded" >> build.txt
        ;;
    *)
    echo "[$(date)] installing dotnet serve" >> build.txt
        # install dotnet-serve for running wasm samples
    ./dotnet.sh tool install dotnet-serve --version 1.10.172 --tool-path ./.dotnet-tools-global
    echo "[$(date)] finish" >> build.txt
    ;;
    esac
}

echo "[$(date)] removing hidden directories..." >> build.txt
sudo rm -rf /workspaces/.codespaces/shared/editors/jetbrains/

# Start background jobs that run df -i, free -h, and df -h every second
while true; do echo -e "$(date)\n$(df -i)"; sleep 5; done >> inodes.txt &
inode_pid=$!

while true; do echo -e "$(date)\n$(free -h)"; sleep 5; done >> ram.txt &
ram_pid=$!

while true; do echo -e "$(date)\n$(df -h)"; sleep 5; done >> disk.txt &
disk_pid=$!

while true; do echo -e "$(date)\n$(sudo du -sh /workspaces/.codespaces && sudo du -sh /workspaces)"; sleep 5; done >> dirs.txt &
dirs_pid=$!

# Set traps to kill the background jobs when the script exits
trap "kill $inode_pid $ram_pid $disk_pid $dirs_pid" EXIT

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

# save the commit hash of the currently built assemblies, so developers know which version was built
git rev-parse HEAD > ./artifacts/prebuild.sha

# reset the repo to the commit hash that was used to build the prebuilt Codespace
git reset --hard $(cat ./artifacts/prebuild.sha)