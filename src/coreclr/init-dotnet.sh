#!/usr/bin/env bash

__scriptpath="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
__RepoRootDir="${__scriptpath}/../.."

# BEGIN SECTION to remove after repo consolidation
if [ ! -f "${__RepoRootDir}/.dotnet-runtime-placeholder" ]; then
  __RepoRootDir=${__scriptpath}
fi
# END SECTION to remove after repo consolidation

echo "Installing dotnet using Arcade..."

source ${__RepoRootDir}/eng/configure-toolset.sh
source ${__RepoRootDir}/eng/common/tools.sh

InitializeBuildTool

if [ $? != 0 ]; then
    echo "Failed to install dotnet using Arcade"
    exit $?
fi