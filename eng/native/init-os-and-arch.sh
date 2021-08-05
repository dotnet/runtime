#!/usr/bin/env bash

# Use uname to determine what the OS is.
OSName=$(uname -s)

if command -v getprop && getprop ro.product.system.model 2>&1 | grep -qi android; then
    OSName="Android"
fi

case "$OSName" in
FreeBSD|Linux|NetBSD|OpenBSD|SunOS|Android)
    os="$OSName" ;;
Darwin)
    os=OSX ;;
*)
    echo "Unsupported OS $OSName detected, configuring as if for Linux"
    os=Linux ;;
esac

# On Solaris, `uname -m` is discouraged, see https://docs.oracle.com/cd/E36784_01/html/E36870/uname-1.html
# and `uname -p` returns processor type (e.g. i386 on amd64).
# The appropriate tool to determine CPU is isainfo(1) https://docs.oracle.com/cd/E36784_01/html/E36870/isainfo-1.html.
if [ "$os" = "SunOS" ]; then
    if uname -o 2>&1 | grep -q illumos; then
        os="illumos"
    else
        os="Solaris"
    fi
    CPUName=$(isainfo -n)
else
    # For the rest of the operating systems, use uname(1) to determine what the CPU is.
    CPUName=$(uname -m)
fi

case "$CPUName" in
    arm64|aarch64)
        arch=arm64
        ;;

    amd64|x86_64)
        arch=x64
        ;;

    armv7l)
        if (NAME=""; . /etc/os-release; test "$NAME" = "Tizen"); then
            arch=armel
        else
            arch=arm
        fi
        ;;

    i[3-6]86)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        arch=x86
        ;;

    s390x)
        arch=s390x
	;;

    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        arch=x64
        ;;
esac
