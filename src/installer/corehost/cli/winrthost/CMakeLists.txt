# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

project(winrthost)

set(DOTNET_PROJECT_NAME "winrthost")

# Include directories
include_directories(../fxr)
include_directories(../fxr/static)

# CMake does not recommend using globbing since it messes with the freshness checks

set (SOURCES
    winrthost.cpp
    ../redirected_error_writer.cpp
    ../fxr_resolver.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

include(../lib.cmake)

add_definitions(-DFEATURE_LIBHOST=1)

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    target_link_libraries(winrthost Advapi32.lib Ole32.lib OleAut32.lib)
endif()

target_link_libraries(winrthost RuntimeObject.lib libhostcommon)

install_with_stripped_symbols(winrthost TARGETS corehost)
