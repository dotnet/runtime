# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(nethost)

set(DOTNET_PROJECT_NAME "nethost")

# Include directories
include_directories(../fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    nethost.cpp
    ../fxr_resolver.cpp
    ../fxr/fx_ver.cpp
)

if(WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

include(../lib.cmake)

add_definitions(-DFEATURE_LIBHOST=1)
add_definitions(-DNETHOST_EXPORT)

install(FILES nethost.h DESTINATION corehost)
install(TARGETS nethost DESTINATION corehost)
install_symbols(nethost corehost)