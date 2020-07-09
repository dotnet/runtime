#!/usr/bin/env bash
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#

set -e

command="$1"
shift
echo "docker $command -u=$(id -u):$(id -g) $@" 
docker "$command" -u="$(id -u):$(id -g)" "$@"
