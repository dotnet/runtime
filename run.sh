#!/usr/bin/env bash

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Running init-tools.sh"
$working_tree_root/init-tools.sh

toolRuntime=$working_tree_root/Tools
dotnet=$toolRuntime/dotnetcli/dotnet

echo "Running: $dotnet $toolRuntime/run.exe $*"
$dotnet $toolRuntime/run.exe $*
if [ $? -ne 0 ]
then
    echo "ERROR: An error occured in $dotnet $toolRuntime/run $#. Check $# logs under $working_tree_root."
    exit 1
fi

echo "Command successfully completed."
exit 0
