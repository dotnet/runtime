# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project (corehost)

if(WIN32)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:/MT>)
    add_compile_options($<$<CONFIG:Release>:/MT>)
    add_compile_options($<$<CONFIG:Debug>:/MTd>)
endif()

include(setup.cmake)

set (CMAKE_CXX_STANDARD 11)

include_directories(../common)
include_directories(.)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../corehost.cpp

    ../common/trace.cpp
    ../common/utils.cpp)


if(WIN32)
    list(APPEND SOURCES ../common/pal.windows.cpp)
else()
    list(APPEND SOURCES ../common/pal.unix.cpp)
endif()

add_executable(corehost ${SOURCES})

# Older CMake doesn't support CMAKE_CXX_STANDARD and GCC/Clang need a switch to enable C++ 11
if(${CMAKE_CXX_COMPILER_ID} MATCHES "(Clang|GNU)")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
endif()

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    add_definitions(-D__LINUX__)
    target_link_libraries (corehost "dl" "pthread")
endif()

add_subdirectory(dll)
