#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)

tasksFolder=~/.nuget/packages/illink.tasks
if [ -d $tasksFolder ]
then
  rm -r $tasksFolder
fi

dotNetTool=$__scriptpath/../corebuild/dotnet.sh
# create integration packages
$dotNetTool restore $__scriptpath/linker.sln
$dotNetTool pack $__scriptpath/linker.sln
