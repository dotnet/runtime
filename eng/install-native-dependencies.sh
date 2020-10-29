#!/usr/bin/env bash

# This is a simple script primarily used for CI to install necessary dependencies
#
# For CI typical usage is
#
# ./install-native-dependencies.sh <OS> <arch> azDO
#
# For developer use it is not recommended to include the azDO final argument as that
# makes installation and configuration setting only required for azDO
#
# So simple developer usage would currently be
#
# ./install-native-dependencies.sh <OS>

if [ "$1" = "OSX" ] && [ "$2" = "arm64" ] && [ "$3" = "azDO" ]; then
    # On AzDO macOS-10.15 build agents the Xcode currently defaults to Xcode 11.7
    # Cross compilation for osx-arm64 requires xcode 12.2 or greater
    sudo xcode-select -s /Applications/Xcode_12.2.app/Contents/Developer
fi

if [ "$1" = "Linux" ]; then
    sudo apt update
    if [ "$?" != "0" ]; then
       exit 1;
    fi
    sudo apt install cmake llvm-3.9 clang-3.9 lldb-3.9 liblldb-3.9-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev libkrb5-dev libnuma-dev autoconf automake libtool build-essential
    if [ "$?" != "0" ]; then
        exit 1;
    fi
elif [ "$1" = "OSX" ] || [ "$1" = "tvOS" ] || [ "$1" = "iOS" ]; then
    engdir=$(dirname "${BASH_SOURCE[0]}")
    brew update --preinstall
    brew bundle --no-upgrade --no-lock --file "${engdir}/Brewfile"
    if [ "$?" != "0" ]; then
        exit 1;
    fi
elif [ "$1" = "Android" ]; then
    if [ -z "${ANDROID_OPENSSL_AAR}" ]; then
        echo "The ANDROID_OPENSSL_AAR variable must be set!"
        exit 1;
    fi
    if [ -d "${ANDROID_OPENSSL_AAR}" ]; then
        exit 0;
    fi
    OPENSSL_VER=1.1.1g-alpha-1
    curl https://maven.google.com/com/android/ndk/thirdparty/openssl/${OPENSSL_VER}/openssl-${OPENSSL_VER}.aar -L --output /tmp/openssl.zip
    unzip /tmp/openssl.zip -d "${ANDROID_OPENSSL_AAR}" && rm -rf /tmp/openssl.zip
else
    echo "Must pass \"Linux\", \"Android\", \"tvOS\", \"iOS\" or \"OSX\" as first argument."
    exit 1
fi

