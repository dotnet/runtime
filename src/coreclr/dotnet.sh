#!/usr/bin/env bash

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
export DOTNET_MULTILEVEL_LOOKUP=0

# Disable first run since we want to control all package sources
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

echo "Running init-dotnet.sh"
source $working_tree_root/init-dotnet.sh

dotnet=${_InitializeDotNetCli}/dotnet

echo "Running: $dotnet $@"
$dotnet "$@"
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred in $dotnet $@. Check logs under $working_tree_root."
    exit 1
fi

echo "Command successfully completed."
exit 0
