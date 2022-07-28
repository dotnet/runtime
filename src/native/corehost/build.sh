#!/usr/bin/env bash

usage_list=("-hostver <Dotnet host version>: Version of the dotnet executable.")
usage_list+=("-apphostver <app host version>: Version of the apphost executable.")
usage_list+=("-fxrver <HostFxr version>: Version of the hostfxr library.")
usage_list+=("-policyver <HostPolicy version>: Version of the hostpolicy library.")
usage_list+=("-commithash <Git commit hash>: Current commit hash of the repo at build time.")

set -e

__scriptpath="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRootDir="$(cd "$__scriptpath"/../../..; pwd -P)"

__TargetArch=x64
__TargetOS=Linux
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
__host_ver=
__apphost_ver=
__policy_ver=
__fxr_ver=
__commit_hash=

handle_arguments() {

    case "$1" in
        hostver|-hostver)
            __host_ver="$2"
            __ShiftArgs=1
            ;;

        apphostver|-apphostver)
            __apphost_ver="$2"
            __ShiftArgs=1
            ;;

        fxrver|-fxrver)
            __fxr_ver="$2"
            __ShiftArgs=1
            ;;

        policyver|-policyver)
            __policy_ver="$2"
            __ShiftArgs=1
            ;;

        commithash|-commithash)
            __commit_hash="$2"
            __ShiftArgs=1
            ;;

        runtimeflavor|-runtimeflavor)
            __RuntimeFlavor="$2"
            __ShiftArgs=1
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
    esac
}

source "$__RepoRootDir"/eng/native/build-commons.sh

# Set dependent variables
__LogsDir="$__RootBinDir/log"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__DistroRidLower="$(echo $__DistroRid | tr '[:upper:]' '[:lower:]')"
__BinDir="$__RootBinDir/bin/$__DistroRidLower.$__BuildType"
__IntermediatesDir="$__RootBinDir/obj/$__DistroRidLower.$__BuildType"

export __BinDir __IntermediatesDir __RuntimeFlavor

__CMakeArgs="-DCLI_CMAKE_HOST_VER=\"$__host_ver\" -DCLI_CMAKE_COMMON_HOST_VER=\"$__apphost_ver\" -DCLI_CMAKE_HOST_FXR_VER=\"$__fxr_ver\" $__CMakeArgs"
__CMakeArgs="-DCLI_CMAKE_HOST_POLICY_VER=\"$__policy_ver\" -DCLI_CMAKE_PKG_RID=\"$__DistroRid\" -DCLI_CMAKE_COMMIT_HASH=\"$__commit_hash\" $__CMakeArgs"
__CMakeArgs="-DRUNTIME_FLAVOR=\"$__RuntimeFlavor\" $__CMakeArgs"
__CMakeArgs="-DFEATURE_DISTRO_AGNOSTIC_SSL=$__PortableBuild $__CMakeArgs"

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built CoreClr libraries will copied to.
__CMakeBinDir="$__BinDir"
export __CMakeBinDir

# Make the directories necessary for build if they don't exist
setup_dirs

# Check prereqs.
check_prereqs

# Build the installer native components.
build_native "$__TargetOS" "$__TargetArch" "$__scriptpath" "$__IntermediatesDir" "install" "$__CMakeArgs" "installer component"
