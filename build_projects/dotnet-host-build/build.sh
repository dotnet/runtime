#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
OLDPATH="$PATH"

REPOROOT="$DIR/../.."
source "$REPOROOT/scripts/common/_prettyprint.sh"

__BuildDriverOnly=0

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        __HostOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        __HostOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        __HostOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        __HostOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        __HostOS=NetBSD
        ;;

    SunOS)
        __BuildOS=SunOS
        __HostOS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        __HostOS=Linux
        ;;
esac

while [[ $# > 0 ]]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -c|--configuration)
            export CONFIGURATION=$2
            shift
            ;;
        --targets)
            IFS=',' read -r -a targets <<< $2
            shift
            ;;
        --env-vars)
            IFS=',' read -r -a envVars <<< $2
            shift
            ;;
        --skiptests)
            export DOTNET_BUILD_SKIP_TESTS=1
            ;;
        --nopackage)
            export DOTNET_BUILD_SKIP_PACKAGING=1
            ;;
        --portablelinux)
            if [ "$__BuildOS" == "Linux" ]; then
                export DOTNET_BUILD_LINK_PORTABLE=1
            else
                echo "ERROR: portableLinux not supported for non-Linux platforms."
                exit 1
            fi
            ;;
        --skip-prereqs)
            # Allow CI to disable prereqs check since the CI has the pre-reqs but not ldconfig it seems
            export DOTNET_INSTALL_SKIP_PREREQS=1
            ;;
        --build-driver-only)
            __BuildDriverOnly=1
            ;;
        --verbose)
            export DOTNET_BUILD_VERBOSE=1
            ;;
        --help)
            echo "Usage: $0 [--configuration <CONFIGURATION>] [--skip-prereqs] [--nopackage] [--docker <IMAGENAME>] [--help] [--targets <TARGETS...>]"
            echo ""
            echo "Options:"
            echo "  --configuration <CONFIGURATION>      Build the specified Configuration (Debug or Release, default: Debug)"
            echo "  --targets <TARGETS...>               Comma separated build targets to run (Init, Compile, Publish, etc.; Default is a full build and publish)"
            echo "  --env-vars <'V1=val1','V2=val2'...>  Comma separated list of environment variables"
            echo "  --nopackage                          Skip packaging targets"
            echo "  --skip-prereqs                       Skip checks for pre-reqs in dotnet_install"
            echo "  --build-driver-only                  Just build dotnet-host-build binary"
            echo "  --portableLinux                      Optional argument to build native libraries portable over GLIBC based Linux distros."
            echo "  --docker <IMAGENAME>                 Build in Docker using the Dockerfile located in scripts/docker/IMAGENAME"
            echo "  --help                               Display this help message"
            echo "  <TARGETS...>                         The build targets to run (Init, Compile, Publish, etc.; Default is a full build and publish)"
            exit 0
            ;;
        *)
            break
            ;;
    esac

    shift
done

# Set nuget package cache under the repo
export NUGET_PACKAGES="$REPOROOT/.nuget/packages"

# Set up the environment to be used for building with clang.
if which "clang-3.5" > /dev/null 2>&1; then
    export CC="$(which clang-3.5)"
    export CXX="$(which clang++-3.5)"
elif which "clang-3.6" > /dev/null 2>&1; then
    export CC="$(which clang-3.6)"
    export CXX="$(which clang++-3.6)"
elif which clang > /dev/null 2>&1; then
    export CC="$(which clang)"
    export CXX="$(which clang++)"
else
    error "Unable to find Clang Compiler"
    error "Install clang-3.5 or clang3.6"
    exit 1
fi

# Load Branch Info
while read line; do
    if [[ $line != \#* ]]; then
        IFS='=' read -ra splat <<< "$line"
        export ${splat[0]}="${splat[1]}"
    fi
done < "$REPOROOT/branchinfo.txt"

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
[ -z "$DOTNET_INSTALL_DIR" ] && export DOTNET_INSTALL_DIR=$REPOROOT/.dotnet_stage0/$(uname)
[ -d "$DOTNET_INSTALL_DIR" ] || mkdir -p $DOTNET_INSTALL_DIR

DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh"
curl -sSL "$DOTNET_INSTALL_SCRIPT_URL" | bash /dev/stdin --version 1.0.0-preview3-003886 --verbose

# Put stage 0 on the PATH (for this shell only)
PATH="$DOTNET_INSTALL_DIR:$PATH"

# Increases the file descriptors limit for this bash. It prevents an issue we were hitting during restore
FILE_DESCRIPTOR_LIMIT=$( ulimit -n )
if [ $FILE_DESCRIPTOR_LIMIT -lt 1024 ]
then
    echo "Increasing file description limit to 1024"
    ulimit -n 1024
fi

# Disable first run since we want to control all package sources
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Restore the build scripts
echo "Restoring Build Script projects..."
(
    cd "$DIR/.."
    dotnet restore --infer-runtimes --disable-parallel
)

# Build the builder
echo "Compiling Build Scripts..."
dotnet publish "$DIR" -o "$DIR/bin" --framework netcoreapp1.0

if [ $__BuildDriverOnly == 1 ]; then
    echo "Skipping invoking build scripts."
    exit 0
fi

export PATH="$OLDPATH"
# Run the builder
echo "Invoking Build Scripts..."
echo "Configuration: $CONFIGURATION"

$DIR/bin/dotnet-host-build -t ${targets[@]} -e ${envVars[@]}
exit $?
