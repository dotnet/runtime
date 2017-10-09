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

# We do not want to run the first-time experience.
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

$__scriptpath/init-tools.sh

__dotnet=$__toolsLocalPath/dotnetcli/dotnet

$__dotnet $__toolsLocalPath/run.exe $*
exit $?
