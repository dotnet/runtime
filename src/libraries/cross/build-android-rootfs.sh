#!/usr/bin/env bash
__NDK_Version=r14

usage()
{
    echo "Creates a toolchain and sysroot used for cross-compiling for Android."
    echo.
    echo "Usage: $0 [BuildArch] [ApiLevel]"
    echo.
    echo "BuildArch is the target architecture of Android. Currently only arm64 is supported."
    echo "ApiLevel is the target Android API level. API levels usually match to Android releases. See https://source.android.com/source/build-numbers.html"
    echo.
    echo "By default, the toolchain and sysroot will be generated in cross/android-rootfs/toolchain/[BuildArch]. You can change this behavior"
    echo "by setting the TOOLCHAIN_DIR environment variable"
    echo.
    echo "By default, the NDK will be downloaded into the cross/android-rootfs/android-ndk-$__NDK_Version directory. If you already have an NDK installation,"
    echo "you can set the NDK_DIR environment variable to have this script use that installation of the NDK."
    exit 1
}

__ApiLevel=21 # The minimum platform for arm64 is API level 21
__BuildArch=arm64
__AndroidArch=aarch64
__AndroidToolchain=aarch64-linux-android

for i in "$@"
    do
        lowerI="$(echo $i | awk '{print tolower($0)}')"
        case $lowerI in
        -?|-h|--help)
            usage
            exit 1
            ;;
        arm64)
            __BuildArch=arm64
            __AndroidArch=aarch64
            __AndroidToolchain=arm-linux-androideabi
            ;;
        arm)
            __BuildArch=arm
            __AndroidArch=arm
            __AndroidToolchain=arm-linux-androideabi
            ;;
        *[0-9])
            __ApiLevel=$i
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
            ;;
    esac
done

# Obtain the location of the bash script to figure out where the root of the repo is.
__CrossDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

__Android_Cross_Dir="$__CrossDir/android-rootfs"
__NDK_Dir="$__Android_Cross_Dir/android-ndk-$__NDK_Version"
__libunwind_Dir="$__Android_Cross_Dir/libunwind"
__ToolchainDir="$__Android_Cross_Dir/toolchain/$__BuildArch"

if [[ -n "$TOOLCHAIN_DIR" ]]; then
    __ToolchainDir=$TOOLCHAIN_DIR
fi

if [[ -n "$NDK_DIR" ]]; then
    __NDK_Dir=$NDK_DIR
fi

echo "Target API level: $__ApiLevel"
echo "Target architecture: $__BuildArch"
echo "NDK location: $__NDK_Dir"
echo "Target Toolchain location: $__ToolchainDir"

# Download the NDK if required
if [ ! -d $__NDK_Dir ]; then
    echo Downloading the NDK into $__NDK_Dir
    mkdir -p $__NDK_Dir
    wget -nv -nc --show-progress https://dl.google.com/android/repository/android-ndk-$__NDK_Version-linux-x86_64.zip -O $__Android_Cross_Dir/android-ndk-$__NDK_Version-linux-x86_64.zip
    unzip -q $__Android_Cross_Dir/android-ndk-$__NDK_Version-linux-x86_64.zip -d $__Android_Cross_Dir
fi

# Create the RootFS for both arm64 as well as aarch
rm -rf $__Android_Cross_Dir/toolchain

echo Generating the $__BuildArch toolchain
$__NDK_Dir/build/tools/make_standalone_toolchain.py --arch $__BuildArch --api $__ApiLevel --install-dir $__ToolchainDir

# Install the required packages into the toolchain
rm -rf $__Android_Cross_Dir/deb/
rm -rf $__Android_Cross_Dir/tmp

mkdir -p $__Android_Cross_Dir/deb/
mkdir -p $__Android_Cross_Dir/tmp/$arch/
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libcurl-dev_7.53.1_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libcurl-dev_7.52.1_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libcurl_7.53.1_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libcurl_7.52.1_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/krb5-dev_1.15.1_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/krb5-dev_1.15_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/krb5_1.15.1_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/krb5_1.15_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/openssl-dev_1.0.2k_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/openssl-dev_1.0.2k_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/openssl_1.0.2k_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/openssl_1.0.2k_$__AndroidArch.deb

echo Unpacking Termux packages
dpkg -x $__Android_Cross_Dir/deb/libcurl-dev_7.52.1_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libcurl_7.52.1_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/krb5-dev_1.15_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/krb5_1.15_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/openssl-dev_1.0.2k_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/openssl_1.0.2k_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/

cp -R $__Android_Cross_Dir/tmp/$__AndroidArch/data/data/com.termux/files/usr/* $__ToolchainDir/sysroot/usr/

# ifaddrs.h is not available natively Android, but a version of it is available
# See https://github.com/termux/termux-packages/issues/338 for context
wget -nv -nc https://raw.githubusercontent.com/qnnnnez/android-ifaddrs/master/include/ifaddrs_single.h -O $__ToolchainDir/sysroot/usr/include/ifaddrs.h

echo Now run:
echo CONFIG_DIR=\`realpath cross/android/$__BuildArch\` ROOTFS_DIR=\`realpath $__ToolchainDir/sysroot\` ./build-native.sh -debug -buildArch=$__BuildArch -- verbose cross
