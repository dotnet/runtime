# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(mockcoreclr)

set(DOTNET_PROJECT_NAME "mockcoreclr")

set(SOURCES
    ./mockcoreclr.cpp
)

if(WIN32)
    list(APPEND SOURCES
        mockcoreclr.def)
endif()

include(../testlib.cmake)

install(TARGETS mockcoreclr DESTINATION corehost_test)
install_symbols(mockcoreclr corehost_test)
