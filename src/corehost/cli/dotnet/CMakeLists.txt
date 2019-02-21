# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(dotnet)
set(DOTNET_PROJECT_NAME "dotnet")
set(SOURCES 
    ../fxr/fx_ver.cpp)

if(WIN32)
    list(APPEND SOURCES
        dotnet.manifest)
endif()

include(../exe.cmake)

install_library_and_symbols (dotnet)
