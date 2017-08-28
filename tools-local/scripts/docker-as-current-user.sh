#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

set -e

command="$1"
shift
echo "docker $command -u=$(id -u):$(id -g) $@" 
docker "$command" -u="$(id -u):$(id -g)" "$@"