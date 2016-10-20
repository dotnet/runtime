# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project (dotnet)

if(WIN32)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:/MT>)
    add_compile_options($<$<CONFIG:Release>:/MT>)
    add_compile_options($<$<CONFIG:Debug>:/MTd>)
else()
    add_compile_options(-fPIE)
endif()

include(../setup.cmake)

# Include directories
if(WIN32)
    include_directories("${CLI_CMAKE_RESOURCE_DIR}/dotnet")
endif()
include_directories(../../)
include_directories(../../common)
include_directories(../)
include_directories(../fxr)
include_directories(../json/casablanca/include)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../fxr/fx_ver.cpp
    ../../corehost.cpp
    ../../common/trace.cpp
    ../../common/utils.cpp)


if(WIN32)
    list(APPEND SOURCES ../../common/pal.windows.cpp)
else()
    list(APPEND SOURCES ../../common/pal.unix.cpp)
endif()

set(RESOURCES)
if(WIN32)
    list(APPEND RESOURCES ../native.rc)
endif()

add_executable(dotnet ${SOURCES} ${RESOURCES})
install(TARGETS dotnet DESTINATION bin)

# Specify the import library to link against for Arm32 build since the default set is minimal
if (WIN32 AND CLI_CMAKE_PLATFORM_ARCH_ARM)
    target_link_libraries(dotnet shell32.lib)
endif()

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    target_link_libraries (dotnet "dl" "pthread")
endif()

