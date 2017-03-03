#!/usr/bin/env bash

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
    echo "By default, the NDK will be downloaded into the cross/android-rootfs/android-ndk-r13b directory. If you already have an NDK installation,"
    echo "you can set the NDK_DIR environment variable to have this script use that installation of the NDK."
    exit 1
}

__ApiLevel=21 # The minimum platform for arm64 is API level 21
__BuildArch=arm64
__AndroidArch=aarch64

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
__NDK_Dir="$__Android_Cross_Dir/android-ndk-r13b"
__libunwind_Dir="$__Android_Cross_Dir/libunwind"
__lldb_Dir="$__Android_Cross_Dir/lldb"
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
    wget -nv -nc --show-progress https://dl.google.com/android/repository/android-ndk-r13b-linux-x86_64.zip -O $__Android_Cross_Dir/android-ndk-r13b-linux-x86_64.zip
    unzip -q $__Android_Cross_Dir/android-ndk-r13b-linux-x86_64.zip -d $__Android_Cross_Dir
fi

if [ ! -d $__lldb_Dir ]; then
    mkdir -p $__lldb_Dir
    echo Downloading LLDB into $__lldb_Dir
    wget -nv -nc --show-progress https://dl.google.com/android/repository/lldb-2.3.3614996-linux-x86_64.zip -O $__Android_Cross_Dir/lldb-2.3.3614996-linux-x86_64.zip
    unzip -q $__Android_Cross_Dir/lldb-2.3.3614996-linux-x86_64.zip -d $__lldb_Dir
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
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libicu_58.2_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libicu_58.2_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libicu-dev_58.2_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libicu-dev_58.2_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libuuid-dev_1.0.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libuuid-dev_1.0.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libuuid_1.0.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libuuid_1.0.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-glob-dev_0.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-glob-dev_0.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-glob_0.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-glob_0.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-support-dev_13.10_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-support-dev_13.10_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-support_13.10_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-support_13.10_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/liblzma-dev_5.2.3_$__AndroidArch.deb  -O $__Android_Cross_Dir/deb/liblzma-dev_5.2.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/liblzma_5.2.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/liblzma_5.2.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libcurl-dev_7.52.1_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libcurl-dev_7.52.1_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libcurl_7.52.1_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libcurl_7.52.1_$__AndroidArch.deb

echo Unpacking Termux packages
dpkg -x $__Android_Cross_Dir/deb/libicu_58.2_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libicu-dev_58.2_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libuuid-dev_1.0.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libuuid_1.0.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-glob-dev_0.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-glob_0.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-support-dev_13.10_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-support_13.10_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/liblzma-dev_5.2.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/liblzma_5.2.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libcurl-dev_7.52.1_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libcurl_7.52.1_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/

cp -R $__Android_Cross_Dir/tmp/$__AndroidArch/data/data/com.termux/files/usr/* $__ToolchainDir/sysroot/usr/

# Prepare libunwind
if [ ! -d $__libunwind_Dir ]; then
# Currently, we clone a fork of libunwind which adds support for Android; once this fork has been
# merged back in, this script can be updated to use the official libunwind repository.
# There's also an Android fork of libunwind which is currently not used.
#   git clone https://android.googlesource.com/platform/external/libunwind/ $__libunwind_Dir
#   git clone https://github.com/libunwind/libunwind/ $__libunwind_Dir
   git clone https://github.com/qmfrederik/libunwind/ $__libunwind_Dir
fi

cd $__libunwind_Dir
git checkout features/android
git checkout -- .
git clean -xfd

# libunwind is available on Android, but not included in the NDK.
echo Building libunwind
autoreconf --force -v --install 2> /dev/null
./configure CC=$__ToolchainDir/bin/$__AndroidArch-linux-android-clang --with-sysroot=$__ToolchainDir/sysroot --host=$__AndroidArch-eabi --target=$__AndroidArch-eabi --disable-tests --disable-coredump --prefix=$__ToolchainDir/sysroot/usr 2> /dev/null
make > /dev/null
make install > /dev/null

# This header file is missing
cp include/libunwind.h $__ToolchainDir/sysroot/usr/include/

echo Now run:
echo CONFIG_DIR=\`realpath cross/android/arm64\` ROOTFS_DIR=\`realpath $__ToolchainDir/sysroot\` ./build.sh cross arm64 skipgenerateversion skipnuget cmakeargs -DENABLE_LLDBPLUGIN=0

