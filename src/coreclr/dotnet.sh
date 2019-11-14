#!/usr/bin/env bash

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
__ProjectDir=${working_tree_root}
__RepoRootDir=${working_tree_root}/../..

# Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
export DOTNET_MULTILEVEL_LOOKUP=0

# Disable first run since we want to control all package sources
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

source ${__RepoRootDir}/eng/common/tools.sh

InitializeDotNetCli
__dotnetDir=${_InitializeDotNetCli}

if [ $? != 0 ]; then
    echo "Failed to install dotnet using Arcade"
    exit $?
fi

dotnetPath=${__dotnetDir}/dotnet

echo "Running: ${dotnetPath} $@"
${dotnetPath} "$@"
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred in ${dotnetPath} $@. Check logs under $working_tree_root."
    exit 1
fi

echo "Command successfully completed."
exit 0
