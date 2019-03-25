# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.8.12)
project(nativehost)

set(DOTNET_PROJECT_NAME "nativehost")

set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
set(MACOSX_RPATH ON)
if (CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(CMAKE_INSTALL_RPATH "@loader_path")
else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
endif()

include_directories(${CMAKE_CURRENT_LIST_DIR}/../../nethost)

set(SOURCES
    ./nativehost.cpp
)

include(../testexe.cmake)

target_link_libraries(${DOTNET_PROJECT_NAME} nethost)