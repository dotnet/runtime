#!/usr/bin/env bash

# Use uname to determine what the OS is.
OSName=$(uname -s)
case "$OSName" in
freebsd|linux|netbsd|openbsd|sunos)
    os=$OSName ;;
Darwin)
	os=osx ;;
*)
    echo "Unsupported OS $OSName detected, configuring as if for linux"
    os=linux ;;
esac

# Use uname to determine what the CPU is.
CPUName=$(uname -m)

case "$CPUName" in
    aarch64)
        arch=arm64
        ;;

    amd64|x86_64)
        arch=x64
        ;;

    armv7l)
        if (NAME=""; . /etc/os-release; test "$NAME" = "Tizen"); then
            __BuildArch=armel
            __HostArch=armel
        else
            __BuildArch=arm
            __HostArch=arm
        fi
        ;;

    i[3-6]86)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        arch=x86
        ;;

    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        arch=x64
        ;;
esac
