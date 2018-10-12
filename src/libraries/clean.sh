#!/usr/bin/env bash

usage()
{
    echo
    echo "Usage: clean [-all]"
    echo "Deletes the binary output directory."
    echo "Options:"
    echo "    -all   - Cleans repository and restores it to pristine state."
    exit 1
}

if [ "$1" == "-?" ] || [ "$1" == "-h" ]; then
    usage
fi

# Implement VBCSCompiler.exe kill logic once VBCSCompiler.exe is ported to unixes

__working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [ "$*" == "-all" ]
then
   echo "Removing all untracked files in the working tree"
   git clean -xdf $__working_tree_root
   exit $?
fi

$__working_tree_root/eng/common/msbuild.sh $__working_tree_root/build.proj /t:Clean "$@"
exit $?
