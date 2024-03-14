#!/usr/bin/env bash

set -e

function wasm_common() {
    make -C src/mono/browser provision-wasm
}

# hidden dir are occupying disc space that could be better used (~12GB)
# active discussion: https://github.com/orgs/community/discussions/57767
sudo rm -rf /workspaces/.codespaces/shared/editors/jetbrains/

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
mkdir -p ./artifacts/
git rev-parse HEAD > ./artifacts/prebuild.sha
