# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
set(DOTNET_HOST_EXE_NAME "apphost")
include(../exe.cmake)
set(SOURCES)
add_definitions(-DFEATURE_APPHOST=1)

