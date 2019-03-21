# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(test_fx_ver)

set(EXE_NAME "test_fx_ver")

include_directories(../fxr)
include_directories(../../common)

set(SOURCES
    test_fx_ver.cpp
    ../fxr/fx_ver.cpp
    ../../common/trace.cpp
    ../../common/utils.cpp)

if(WIN32)
    list(APPEND SOURCES
        ../../common/pal.windows.cpp
        ../../common/longfile.windows.cpp)
else()
    list(APPEND SOURCES
        ../../common/pal.unix.cpp)
endif()

if(WIN32)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:/MT>)
    add_compile_options($<$<CONFIG:Release>:/MT>)
    add_compile_options($<$<CONFIG:Debug>:/MTd>)
else()
    add_compile_options(-fPIE)
    add_compile_options(-fvisibility=hidden)
endif()

add_executable(${EXE_NAME} ${SOURCES})

install(TARGETS ${EXE_NAME} DESTINATION corehost_test)

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    target_link_libraries (${EXE_NAME} "dl")
endif()

if((${CMAKE_SYSTEM_NAME} MATCHES "Linux") AND CLI_CMAKE_PLATFORM_ARCH_ARM)
    target_link_libraries (${EXE_NAME} "atomic")
endif()
