#!/usr/bin/env bash

set -e

opt=$1
case "$opt" in
    android)
        # Create the Android emulator.
        ${ANDROID_SDK_ROOT}/cmdline-tools/cmdline-tools/bin/avdmanager -s create avd --name ${EMULATOR_NAME_X64} --package "system-images;android-${SDK_API_LEVEL};default;x86_64"
    ;;
esac

# reset the repo to the commit hash that was used to build the prebuilt Codespace
git reset --hard $(cat ./artifacts/prebuild.sha)
