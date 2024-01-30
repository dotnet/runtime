#!/usr/bin/env bash

usage_list=("-outconfig: Configuration, typically a quadruplet such as 'net8.0-linux-Release-x64', used to name output directory.")
usage_list+=("-staticLibLink: Optional argument to statically link any native library.")

__scriptpath="$(cd "$(dirname "$0")"; pwd -P)"
__nativeroot="$__scriptpath"
__RepoRootDir="$(cd "$__scriptpath"/../../..; pwd -P)"
__artifactsDir="$__RepoRootDir/artifacts"

handle_arguments() {

    case "$1" in
        outconfig|-outconfig)
            __outConfig="$2"
            __ShiftArgs=1
            ;;

        icudir|-icudir)
            __icuDir="$2"
            __ShiftArgs=1
            ;;

        usepthreads|-usepthreads)
            __usePThreads=1
            ;;

        staticliblink|-staticliblink)
            __StaticLibLink=1
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
    esac
}

# Set the various build properties here so that CMake and MSBuild can pick them up
__TargetArch=x64
__TargetOS=linux
__BuildType=Debug
__CMakeArgs=""
__Compiler=clang
__CrossBuild=0
__PortableBuild=1
__RootBinDir="$__RepoRootDir/artifacts"
__SkipConfigure=0
__StaticLibLink=0
__UnprocessedBuildArgs=
__VerboseBuild=false
__icuDir=""
__usePThreads=0

source "$__RepoRootDir"/eng/native/build-commons.sh

# Set cross build
if [[ "$__TargetOS" == browser ]]; then
    if [[ -z "$EMSDK_PATH" ]]; then
        if [[ -d "$__RepoRootDir"/src/mono/browser/emsdk/ ]]; then
            export EMSDK_PATH="$__RepoRootDir"/src/mono/browser/emsdk/
        else
            echo "Error: You need to set the EMSDK_PATH environment variable pointing to the emscripten SDK root."
            exit 1
        fi
    fi
    source "$EMSDK_PATH"/emsdk_env.sh
    export CLR_CC=$(which emcc)
elif [[ "$__TargetOS" == wasi ]]; then
    if [[ -z "$WASI_SDK_PATH" ]]; then
        if [[ -d "$__RepoRootDir"/src/mono/wasi/wasi-sdk ]]; then
            export WASI_SDK_PATH="$__RepoRootDir"/src/mono/wasi/wasi-sdk
        else
            echo "Error: You need to set the WASI_SDK_PATH environment variable pointing to the WASI SDK root."
            exit 1
        fi
    fi
    export WASI_SDK_PATH="${WASI_SDK_PATH%/}/"
    export CLR_CC="$WASI_SDK_PATH"bin/clang
    export TARGET_BUILD_ARCH=wasm
    __CMakeArgs="-DCLR_CMAKE_TARGET_OS=wasi -DCLR_CMAKE_TARGET_ARCH=wasm -DWASI_SDK_PREFIX=$WASI_SDK_PATH -DCMAKE_TOOLCHAIN_FILE=$WASI_SDK_PATH/share/cmake/wasi-sdk.cmake $__CMakeArgs"
elif [[ "$__TargetOS" == ios || "$__TargetOS" == iossimulator ]]; then
    # nothing to do here
    true
elif [[ "$__TargetOS" == tvos || "$__TargetOS" == tvossimulator ]]; then
    # nothing to do here
    true
elif [[ "$__TargetOS" == android && -z "$ROOTFS_DIR" ]]; then
    # nothing to do here
    true
else
    __CMakeArgs="-DFEATURE_DISTRO_AGNOSTIC_SSL=$__PortableBuild $__CMakeArgs"
    __CMakeArgs="-DCMAKE_STATIC_LIB_LINK=$__StaticLibLink $__CMakeArgs"

    if [[ "$__TargetOS" != linux-bionic && "$__TargetArch" != x86 && "$__TargetArch" != x64 && "$__TargetArch" != "$__HostArch" ]]; then
        __CrossBuild=1
        echo "Set CrossBuild for $__TargetArch build"
    fi
fi

if [[ "$__TargetOS" == android && -z "$ROOTFS_DIR" ]]; then
    # Android SDK defaults to c++_static; we only need C support
    __CMakeArgs="-DANDROID_STL=none $__CMakeArgs"
elif [[ "$__TargetOS" == linux-bionic && -z "$ROOTFS_DIR" ]]; then
    # Android SDK defaults to c++_static; we only need C support
    __CMakeArgs="-DFORCE_ANDROID_OPENSSL=1 -DANDROID_STL=none -DANDROID_FORCE_ICU_DATA_DIR=1 $__CMakeArgs"
elif [[ "$__TargetOS" == iossimulator ]]; then
    # set default iOS simulator deployment target
    # keep in sync with SetOSTargetMinVersions in the root Directory.Build.props
    __CMakeArgs="-DCMAKE_SYSTEM_NAME=iOS -DCMAKE_OSX_SYSROOT=iphonesimulator -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0 $__CMakeArgs"
    if [[ "$__TargetArch" == x64 ]]; then
        __CMakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"x86_64\" $__CMakeArgs"
    elif [[ "$__TargetArch" == x86 ]]; then
        __CMakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"i386\" $__CMakeArgs"
    elif [[ "$__TargetArch" == arm64 ]]; then
        __CMakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"arm64\" $__CMakeArgs"
    else
        echo "Error: Unknown iOS Simulator architecture $__TargetArch."
        exit 1
    fi
elif [[ "$__TargetOS" == ios ]]; then
    # set default iOS device deployment target
    # keep in sync with SetOSTargetMinVersions in the root Directory.Build.props
    __CMakeArgs="-DCMAKE_SYSTEM_NAME=iOS -DCMAKE_OSX_SYSROOT=iphoneos -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0 $__CMakeArgs"
    if [[ "$__TargetArch" == arm64 ]]; then
        __CMakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"arm64\" $__CMakeArgs"
    elif [[ "$__TargetArch" == arm ]]; then
        __CMakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"armv7;armv7s\" $__CMakeArgs"
    else
        echo "Error: Unknown iOS architecture $__TargetArch."
        exit 1
    fi
elif [[ "$__TargetOS" == tvossimulator ]]; then
    # set default tvOS simulator deployment target
    # keep in sync with SetOSTargetMinVersions in the root Directory.Build.props
    __CMakeArgs="-DCMAKE_SYSTEM_NAME=tvOS -DCMAKE_OSX_SYSROOT=appletvsimulator -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0 $__CMakeArgs"
    if [[ "$__TargetArch" == x64 ]]; then
        __CMakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"x86_64\" $__CMakeArgs"
    elif [[ "$__TargetArch" == arm64 ]]; then
        __CMakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"arm64\" $__CMakeArgs"
    else
        echo "Error: Unknown tvOS Simulator architecture $__TargetArch."
        exit 1
    fi
elif [[ "$__TargetOS" == tvos ]]; then
    # set default tvOS device deployment target
    # keep in sync with the root Directory.Build.props
    __CMakeArgs="-DCMAKE_SYSTEM_NAME=tvOS -DCMAKE_OSX_SYSROOT=appletvos -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0 $__CMakeArgs"
    if [[ "$__TargetArch" == arm64 ]]; then
        __CMakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"arm64\" $__CMakeArgs"
    else
        echo "Error: Unknown tvOS architecture $__TargetArch."
        exit 1
    fi
fi

if [[ -n "$__icuDir" ]]; then
    __CMakeArgs="-DCMAKE_ICU_DIR=\"$__icuDir\" $__CMakeArgs"
fi
__CMakeArgs="-DCMAKE_USE_PTHREADS=$__usePThreads $__CMakeArgs"

# Set the remaining variables based upon the determined build configuration
__outConfig="${__outConfig:-"$__TargetOS-$__TargetArch-$__BuildType"}"
__IntermediatesDir="$__RootBinDir/obj/native/$__outConfig"
__BinDir="$__RootBinDir/bin/native/$__outConfig"

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built CoreClr libraries will copied to.
__CMakeBinDir="$__BinDir"
export __CMakeBinDir

# Make the directories necessary for build if they don't exist
setup_dirs

# Check prereqs.
check_prereqs

# Build the corefx native components.
build_native "$__TargetOS" "$__TargetArch" "$__nativeroot" "$__IntermediatesDir" "install" "$__CMakeArgs" "native libraries component"
