#!/usr/bin/env sh

if [ "$1" = "linux" ]; then
    sudo apt update
    if [ "$?" != "0" ]; then
       exit 1;
    fi
    sudo apt install cmake llvm-3.9 clang-3.9 lldb-3.9 liblldb-3.9-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev libkrb5-dev libnuma-dev autoconf automake libtool build-essential
    if [ "$?" != "0" ]; then
        exit 1;
    fi
elif [ "$1" = "osx" ]; then
    brew update
    brew upgrade
    if [ "$?" != "0" ]; then
        exit 1;
    fi
    brew install icu4c openssl autoconf automake libtool pkg-config python3
    if [ "$?" != "0" ]; then
        exit 1;
    fi
    brew link --force icu4c
    if [ "$?" != "0" ]; then
        exit 1;
    fi
else
    echo "Must pass \"linux\" or \"osx\" as first argument."
    exit 1
fi

