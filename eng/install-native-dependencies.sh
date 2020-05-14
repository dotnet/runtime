#!/usr/bin/env sh

if [ "$1" = "Linux" ]; then
    sudo apt update
    if [ "$?" != "0" ]; then
       exit 1;
    fi
    sudo apt install cmake llvm-3.9 clang-3.9 lldb-3.9 liblldb-3.9-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev libkrb5-dev libnuma-dev autoconf automake libtool build-essential
    if [ "$?" != "0" ]; then
        exit 1;
    fi
elif [ "$1" = "OSX" ]; then
    brew update
    brew upgrade
    if [ "$?" != "0" ]; then
        exit 1;
    fi
    brew install autoconf automake icu4c libtool openssl@1.1 pkg-config python3
    if [ "$?" != "0" ]; then
        exit 1;
    fi
    if [ "$?" != "0" ]; then
        exit 1;
    fi
elif [ "$1" = "tvOS" ]; then
    brew update
    brew upgrade
    if [ "$?" != "0" ]; then
        exit 1;
    fi
    brew install autoconf automake libtool openssl@1.1 pkg-config python3
    if [ "$?" != "0" ]; then
        exit 1;
    fi
elif [ "$1" = "iOS" ]; then
    brew update
    brew upgrade
    if [ "$?" != "0" ]; then
        exit 1;
    fi
    brew install autoconf automake libtool openssl@1.1 pkg-config python3
    if [ "$?" != "0" ]; then
        exit 1;
    fi
else
    echo "Must pass \"Linux\", \"tvOS\", \"iOS\" or \"OSX\" as first argument."
    exit 1
fi

