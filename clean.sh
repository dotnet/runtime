#!/usr/bin/env bash
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
set -e

usage()
{
    echo "Usage: clean [options]"
    echo "Cleans the local dev environment."
    echo
    echo "  -b         Delete the binary output directory."
    echo "  -p         Delete the repo-local NuGet package directory."
    echo "  -c         Delete the user-local NuGet package caches."
    echo "  -all       Cleans the root directory."
    echo
    echo "If no option is specified, then \"clean.sh -b\" is implied."
    exit 1
}

if [ "$1" == "-?" ] || [ "$1" == "-h" ]; then
    usage
fi

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