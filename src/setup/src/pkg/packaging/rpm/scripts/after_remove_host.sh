#!/bin/sh
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Run the script only when newer version is not installed. Skip during package upgrade 
if [ $1 = 0 ]; then
   echo "Removing dotnet host symbolic link"
   unlink /usr/bin/dotnet
fi