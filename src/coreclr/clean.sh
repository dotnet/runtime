#!/usr/bin/env bash

usage()
{
    echo "Usage: clean [-b] [-p] [-c] [-all]"
    echo "Repository cleaning script."
    echo "  -b         Delete the binary output directory."
    echo "  -p         Delete the repo-local NuGet package directory."
    echo "  -c         Delete the user-local NuGet package caches."
    echo "  -all       Cleans repository and restores it to pristine state."
    echo
    echo "If no option is specified, then \"clean.sh -b\" is implied."
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

if [ $# == 0 ]; then
    __args=-b
fi

$__working_tree_root/run.sh clean $__args $*
exit $?
