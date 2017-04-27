#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

set -e

# Use in the the functions: eval $invocation
invocation='echo "Calling: ${FUNCNAME[0]}"'
__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__toolsLocalPath=$__scriptpath/Tools

$__scriptpath/init-tools.sh

__dotnet=$__toolsLocalPath/dotnetcli/dotnet

cp -fR $__scriptpath/tools-override/* $__toolsLocalPath 

$__dotnet $__toolsLocalPath/run.exe $*
exit $?