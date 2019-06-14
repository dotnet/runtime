#!/usr/bin/env bash

__scriptpath="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Installing dotnet using Arcade..."

source ${__scriptpath}/eng/configure-toolset.sh
source ${__scriptpath}/eng/common/tools.sh

InitializeBuildTool

if [ $? != 0 ]; then
    echo "Failed to install dotnet using Arcade"
    exit $?
fi