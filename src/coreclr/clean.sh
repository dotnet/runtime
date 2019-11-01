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
elif  [ $# == 0 ] || [ "$*" == "-b" ]; then
    __args="/t:CleanAllProjects"
elif  [ "$*" == "-p" ]; then
    __args="/t:CleanPackages"
elif  [ "$*" == "-c" ]; then
    __args="/t:CleanPackagesCache"
fi


$__working_tree_root/dotnet.sh msbuild /nologo /verbosity:minimal /clp:Summary /flp:v=normal\;LogFile=clean.log $__args
exit $?
