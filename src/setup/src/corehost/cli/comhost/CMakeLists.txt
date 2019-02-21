# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(comhost)

set(DOTNET_PROJECT_NAME "comhost")

# Include directories
include_directories(../fxr)
include_directories(../json/casablanca/include)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../../corehost.cpp
    exports.cpp
    clsidmap.cpp
    ../../common/trace.cpp
    ../../common/utils.cpp
    ../fxr/fx_ver.cpp
    ../json/casablanca/src/json/json.cpp
    ../json/casablanca/src/json/json_parsing.cpp
    ../json/casablanca/src/json/json_serialization.cpp
    ../json/casablanca/src/utilities/asyncrt_utils.cpp
)

include(../lib.cmake)

# Move to setup.cmake if COMHOST is ever built on non-Windows
if("${CLI_CMAKE_COMHOST_VER}" STREQUAL "")
    message(FATAL_ERROR "comhost version is not specified")
else()
    add_definitions(-DLIBHOST_PKG_VER="${CLI_CMAKE_COMHOST_VER}")
endif()

add_definitions(-DFEATURE_LIBHOST=1)

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (WIN32 AND (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64))
    target_link_libraries(comhost Advapi32.lib Ole32.lib OleAut32.lib)
endif()

install_library_and_symbols (comhost)