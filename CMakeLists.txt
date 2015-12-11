# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project (corehost)
set (CMAKE_CXX_STANDARD 11)
include_directories(inc)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    src/args.cpp
    src/main.cpp
    src/deps_resolver.cpp
    src/trace.cpp
    src/utils.cpp
    src/coreclr.cpp
    src/servicing_index.cpp

    inc/args.h
    inc/pal.h
    inc/servicing_index.h
    inc/deps_resolver.h
    inc/trace.h
    inc/coreclr.h
    inc/utils.h)

if(WIN32)
    list(APPEND SOURCES src/pal.windows.cpp)
else()
    list(APPEND SOURCES src/pal.unix.cpp)
endif()

add_executable(corehost ${SOURCES})

# Older CMake doesn't support CMAKE_CXX_STANDARD and GCC/Clang need a switch to enable C++ 11
if(${CMAKE_CXX_COMPILER_ID} MATCHES "(Clang|GNU)")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
endif()

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    add_definitions(-D__LINUX__)
    target_link_libraries (corehost "dl")
endif()
