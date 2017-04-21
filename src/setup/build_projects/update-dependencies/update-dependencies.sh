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
PROJECTARGS=""
source "$REPOROOT/scripts/common/_prettyprint.sh"

while [[ $# > 0 ]]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        --update)
            PROJECTARGS="--update"
            ;;
        --env-vars)
            IFS=',' read -r -a envVars <<< $2
            shift
            ;;
        --help)
            echo "Usage: $0"
            echo ""
            echo "Options:"
            echo "  --update                             Update dependencies (but don't open a PR)"
            echo "  --env-vars <'V1=val1','V2=val2'...>  Comma separated list of environment variable name value-pairs"
            echo "  --help                               Display this help message"
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

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
[ -z "$DOTNET_INSTALL_DIR" ] && export DOTNET_INSTALL_DIR=$REPOROOT/.dotnet_stage0/$(uname)
[ -d "$DOTNET_INSTALL_DIR" ] || mkdir -p $DOTNET_INSTALL_DIR

DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh"
curl -sSL "$DOTNET_INSTALL_SCRIPT_URL" | bash /dev/stdin --version 1.0.0-preview3-003886 --verbose

# Put stage 0 on the PATH (for this shell only)
PATH="$DOTNET_INSTALL_DIR:$PATH"

# Figure out the RID of the current platform, based on what stage 0 thinks.
RID=$(dotnet --info | grep 'RID:' | sed -e 's/[[:space:]]*RID:[[:space:]]*\(.*\)/\1/g')

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
    pushd "$DIR/.."
    sed -e "s/{RID}/$RID/g" "dotnet-host-build/project.json.template" > "dotnet-host-build/project.json"
    sed -e "s/{RID}/$RID/g" "update-dependencies/project.json.template" > "update-dependencies/project.json"

    dotnet restore --disable-parallel
    popd
)

# Build the builder
echo "Compiling Build Scripts..."
dotnet publish "$DIR" -o "$DIR/bin" --framework netcoreapp1.0

export PATH="$OLDPATH"
# Run the builder
echo "Invoking Build Scripts..."
echo "Configuration: $CONFIGURATION"

$DIR/bin/update-dependencies "$PROJECTARGS" -e ${envVars[@]}
exit $?
