#!/bin/sh
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
echo "Creating dotnet host symbolic link: /usr/bin/dotnet"
ln -sf "/usr/share/dotnet/dotnet" "/usr/bin/dotnet"