#!/usr/bin/env bash

usage_list=("-project <project name>: Name of the vendored project to build.")
usage_list+=("-flavor <name>: Name of the project flavor.")

set -e

__scriptpath="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRootDir="$(cd "$__scriptpath"/../../..; pwd -P)"

__TargetArch=x64
__TargetOS=linux
__BuildType=Debug
__CMakeArgs=""
__Compiler=clang
__CrossBuild=0
__PortableBuild=1
__RootBinDir="$__RepoRootDir/artifacts"
__SkipConfigure=0
__UnprocessedBuildArgs=
__VerboseBuild=false
__project=
__flavor=

handle_arguments() {

    case "$1" in
        project|-project)
            __project="$2"
            __ShiftArgs=1
            ;;

        flavor|-flavor)
            __flavor="$2"
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
__BinDir="$__RootBinDir/bin/native/external/$__TargetOS.$__TargetArch.$__BuildType/$__flavor"
__IntermediatesDir="$__RootBinDir/obj/native/external/$__project/$__TargetOS.$__TargetArch.$__BuildType/$__flavor"

export __BinDir __IntermediatesDir

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built CoreClr libraries will copied to.
__CMakeBinDir="$__BinDir"
export __CMakeBinDir

# Make the directories necessary for build if they don't exist
setup_dirs

# Check prereqs.
check_prereqs

# Build the installer native components.
build_native "$__TargetOS" "$__TargetArch" "$__scriptpath" "$__IntermediatesDir" "install" "$__CMakeArgs" "vendored libraries"
