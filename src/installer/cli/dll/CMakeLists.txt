# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(hostpolicy)

if(WIN32)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:/MT>)
    add_compile_options($<$<CONFIG:Release>:/MT>)
    add_compile_options($<$<CONFIG:Debug>:/MTd>)
endif()

include(../setup.cmake)

include_directories(../../common)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../../common/trace.cpp
    ../../common/utils.cpp

    ../args.cpp
    ../hostpolicy.cpp
    ../coreclr.cpp
    ../deps_resolver.cpp
    ../servicing_index.cpp)


if(WIN32)
    list(APPEND SOURCES ../../common/pal.windows.cpp)
else()
    list(APPEND SOURCES ../../common/pal.unix.cpp)
endif()

add_definitions(-DCOREHOST_MAKE_DLL=1)

add_library(hostpolicy SHARED ${SOURCES})

