#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)

tasksFolder=~/.nuget/packages/illink.tasks
if [ -d $tasksFolder ]
then
  rm -r $tasksFolder
fi

dotNetTool=$__scriptpath/../../eng/dotnet.sh
# create integration packages
$dotNetTool restore $__scriptpath/../../illink.sln
$dotNetTool pack $__scriptpath/../../illink.sln
