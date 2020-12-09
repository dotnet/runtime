#!/usr/bin/env bash

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

    # run this only on AzDo
    if [ "$BUILD_BUILDNUMBER" != "" ]; then
        # workaround for old osx images on hosted agents
        # piped in case we get an agent without these values installed
        if ! brew_output="$(brew uninstall openssl@1.0.2t 2>&1 >/dev/null)"; then
            echo "didn't uninstall openssl@1.0.2t"
        else
            echo "succesfully uninstalled openssl@1.0.2t"
        fi
    fi

    brew update --preinstall
    brew bundle --no-upgrade --no-lock --file "${engdir}/Brewfile"
    if [ "$?" != "0" ]; then
        exit 1;
    fi
else
    echo "Must pass \"Linux\", \"tvOS\", \"iOS\" or \"OSX\" as first argument."
    exit 1
fi

