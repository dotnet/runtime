# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(nethost)

set(DOTNET_PROJECT_NAME "nethost")

# Include directories
include_directories(../fxr)
include_directories(../fxr/static)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    nethost.cpp
    ../fxr_resolver.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

include(../lib.cmake)
include(../lib_static.cmake)

add_definitions(-DFEATURE_LIBHOST=1)
add_definitions(-DNETHOST_EXPORT)

install(FILES nethost.h DESTINATION corehost)
install(TARGETS libnethost DESTINATION corehost)
install_with_stripped_symbols(nethost TARGETS corehost)
