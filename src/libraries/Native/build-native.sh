#!/usr/bin/env bash

usage_list=("-outconfig: Configuration, typically a quadruplet such as 'net6.0-Linux-Release-x64', used to name output directory.")
usage_list+=("-staticLibLink: Optional argument to statically link any native library.")

__scriptpath="$(cd "$(dirname "$0")"; pwd -P)"
__nativeroot="$__scriptpath"/Unix
__RepoRootDir="$(cd "$__scriptpath"/../../..; pwd -P)"
__artifactsDir="$__RepoRootDir/artifacts"

handle_arguments() {

    case "$1" in
        outconfig|-outconfig)
            __outConfig="$2"
            __ShiftArgs=1
            ;;

        staticliblink|-staticliblink)
            __StaticLibLink=1
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
    esac
}

# Set the various build properties here so that CMake and MSBuild can pick them up
__BuildArch=x64
__TargetOS=Linux
__BuildType=Debug
__CMakeArgs=""
__Compiler=clang
__CompilerMajorVersion=
__CompilerMinorVersion=
__CrossBuild=0
__IsMSBuildOnNETCoreSupported=0
__PortableBuild=1
__RootBinDir="$__RepoRootDir/artifacts"
__SkipConfigure=0
__SkipGenerateVersion=0
__StaticLibLink=0
__UnprocessedBuildArgs=
__VerboseBuild=false

source "$__RepoRootDir"/eng/native/build-commons.sh

# Set cross build

if [[ "$__BuildArch" == wasm ]]; then
    if [[ -z "$EMSDK_PATH" ]]; then
        echo "Error: You need to set the EMSDK_PATH environment variable pointing to the emscripten SDK root."
        exit 1
    fi
    source "$EMSDK_PATH"/emsdk_env.sh

    export CLR_CC=$(which emcc)
    export CLR_CXX=$(which em++)
elif [[ "$__TargetOS" == iOS ]]; then
    # nothing to do here
    true
elif [[ "$__TargetOS" == tvOS ]]; then
    # nothing to do here
    true
elif [[ "$__TargetOS" == Android && -z "$ROOTFS_DIR" ]]; then
    # nothing to do here
    true
else
    __CMakeArgs="-DFEATURE_DISTRO_AGNOSTIC_SSL=$__PortableBuild $__CMakeArgs"
    __CMakeArgs="-DCMAKE_STATIC_LIB_LINK=$__StaticLibLink $__CMakeArgs"

    if [[ "$__BuildArch" != x86 && "$__BuildArch" != x64 && "$__BuildArch" != "$__HostArch" ]]; then
        __CrossBuild=1
        echo "Set CrossBuild for $__BuildArch build"
    fi
fi

if [[ "$__TargetOS" == OSX ]]; then
    # set default OSX deployment target
    __CMakeArgs="-DCMAKE_OSX_DEPLOYMENT_TARGET=10.13 $__CMakeArgs"
elif [[ "$__TargetOS" == Android && -z "$ROOTFS_DIR" ]]; then
    if [[ -z "$ANDROID_NDK_ROOT" ]]; then
        echo "Error: You need to set the ANDROID_NDK_ROOT environment variable pointing to the Android NDK root."
        exit 1
    fi

    # keep ANDROID_NATIVE_API_LEVEL in sync with src/mono/Directory.Build.props
    __CMakeArgs="-DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_STL=none -DANDROID_NATIVE_API_LEVEL=21 $__CMakeArgs"

    # workaround init-compiler.sh trying to detect clang, it's handled in android.toolchain.cmake already
    export CLR_CC=$(which false)
    export CLR_CXX=$(which false)

    if [[ "$__BuildArch" == x64 ]]; then
        __CMakeArgs="-DANDROID_ABI=x86_64 $__CMakeArgs"
    elif [[ "$__BuildArch" == x86 ]]; then
        __CMakeArgs="-DANDROID_ABI=x86 $__CMakeArgs"
    elif [[ "$__BuildArch" == arm64 ]]; then
        __CMakeArgs="-DANDROID_ABI=arm64-v8a $__CMakeArgs"
    elif [[ "$__BuildArch" == arm ]]; then
        __CMakeArgs="-DANDROID_ABI=armeabi-v7a $__CMakeArgs"
    else
        echo "Error: Unknown Android architecture $__BuildArch."
        exit 1
    fi
elif [[ "$__TargetOS" == iOS ]]; then
    __CMakeArgs="-DCMAKE_SYSTEM_NAME=iOS $__CMakeArgs"
    if [[ "$__BuildArch" == x64 ]]; then
        # set default iOS simulator deployment target (8.0 is the minimum supported by Xcode 11)
        # keep in sync with src/mono/Directory.Build.props
        __CMakeArgs="-DCMAKE_OSX_SYSROOT=iphonesimulator -DCMAKE_OSX_DEPLOYMENT_TARGET=8.0 -DCMAKE_OSX_ARCHITECTURES=\"x86_64\" $__CMakeArgs"
    elif [[ "$__BuildArch" == x86 ]]; then
        # set default iOS simulator deployment target (8.0 is the minimum supported by Xcode 11)
        # keep in sync with src/mono/Directory.Build.props
        __CMakeArgs="-DCMAKE_OSX_SYSROOT=iphonesimulator -DCMAKE_OSX_DEPLOYMENT_TARGET=8.0 -DCMAKE_OSX_ARCHITECTURES=\"i386\" $__CMakeArgs"
    elif [[ "$__BuildArch" == arm64 ]]; then
        # set default iOS device deployment target
        # keep in sync with src/mono/Directory.Build.props
        __CMakeArgs="-DCMAKE_OSX_SYSROOT=iphoneos -DCMAKE_OSX_DEPLOYMENT_TARGET=8.0 -DCMAKE_OSX_ARCHITECTURES=\"arm64\" $__CMakeArgs"
    elif [[ "$__BuildArch" == arm ]]; then
        # set default iOS device deployment target
        # keep in sync with src/mono/Directory.Build.props
        __CMakeArgs="-DCMAKE_OSX_SYSROOT=iphoneos -DCMAKE_OSX_DEPLOYMENT_TARGET=8.0 -DCMAKE_OSX_ARCHITECTURES=\"armv7;armv7s\" $__CMakeArgs"
    else
        echo "Error: Unknown iOS architecture $__BuildArch."
        exit 1
    fi
elif [[ "$__TargetOS" == tvOS ]]; then
    __CMakeArgs="-DCMAKE_SYSTEM_NAME=tvOS $__CMakeArgs"
    # set default tvOS device deployment target 
    # keep in sync with tvOSVersionMin in src/mono/Directory.Build.props
    __CMakeArgs="-DCMAKE_OSX_DEPLOYMENT_TARGET=9.0 $__CMakeArgs"
    if [[ "$__BuildArch" == x64 ]]; then
        __CMakeArgs="-DCMAKE_OSX_SYSROOT=appletvsimulator -DCMAKE_OSX_ARCHITECTURES=\"x86_64\" $__CMakeArgs"
    elif [[ "$__BuildArch" == arm64 ]]; then
        __CMakeArgs="-DCMAKE_OSX_SYSROOT=appletvos -DCMAKE_OSX_ARCHITECTURES=\"arm64\" $__CMakeArgs"
    else
        echo "Error: Unknown tvOS architecture $__BuildArch."
        exit 1
    fi
fi

# Set the remaining variables based upon the determined build configuration
__outConfig="${__outConfig:-"$__TargetOS-$__BuildArch-$__BuildType"}"
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
build_native "$__TargetOS" "$__BuildArch" "$__nativeroot" "$__IntermediatesDir" "$__CMakeArgs" "native libraries component"
