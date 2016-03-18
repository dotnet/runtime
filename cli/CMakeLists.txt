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

include_directories(..)
include_directories(../common)
include_directories(.)
include_directories(./fxr)
include_directories(./json/casablanca/include)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    libhost.cpp

    #deps_format.cpp
    #./json/casablanca/src/json/json.cpp
    #./json/casablanca/src/json/json_parsing.cpp
    #./json/casablanca/src/json/json_serialization.cpp
    #./json/casablanca/src/utilities/asyncrt_utils.cpp


    ./fxr/fx_ver.cpp
    ../corehost.cpp
    ../policy_load.cpp
    ../common/trace.cpp
    ../common/utils.cpp)


if(WIN32)
    list(APPEND SOURCES ../common/pal.windows.cpp)
else()
    list(APPEND SOURCES ../common/pal.unix.cpp)
endif()

add_executable(corehost ${SOURCES})
install(TARGETS corehost DESTINATION bin)
add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)

# Older CMake doesn't support CMAKE_CXX_STANDARD and GCC/Clang need a switch to enable C++ 11
if(${CMAKE_CXX_COMPILER_ID} MATCHES "(Clang|GNU)")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11")
endif()

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    add_definitions(-D__LINUX__)
    target_link_libraries (corehost "dl" "pthread")
endif()

add_subdirectory(dll)
add_subdirectory(fxr)
