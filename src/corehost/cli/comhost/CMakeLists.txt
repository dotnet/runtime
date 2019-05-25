# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(comhost)

set(DOTNET_PROJECT_NAME "comhost")

# Include directories
include_directories(../fxr)
include_directories(../json/casablanca/include)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    comhost.cpp
    ../fxr_resolver.cpp
    clsidmap.cpp
    ../redirected_error_writer.cpp
    ../fxr/fx_ver.cpp
    ../json/casablanca/src/json/json.cpp
    ../json/casablanca/src/json/json_parsing.cpp
    ../json/casablanca/src/json/json_serialization.cpp
    ../json/casablanca/src/utilities/asyncrt_utils.cpp
)

set(HEADERS
    comhost.h
    ../fxr/fx_ver.h
    ../json/casablanca/include/cpprest/json.h
)

if(WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

include(../lib.cmake)

add_definitions(-DFEATURE_LIBHOST=1)

if (WIN32)
    set(WINLIBS wintrust.lib)

    # Specify non-default Windows libs to be used for Arm/Arm64 builds
    if (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64)
        list(APPEND WINLIBS Advapi32.lib Ole32.lib OleAut32.lib)
    endif()

    target_link_libraries(comhost ${WINLIBS})
endif()

install(TARGETS comhost DESTINATION corehost)
install_symbols(comhost corehost)