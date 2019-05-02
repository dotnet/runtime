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
    ./host_context_test.cpp
    ./nativehost.cpp
)

if(WIN32)
    list(APPEND SOURCES
        ./comhost_test.cpp)

    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DELAYLOAD:nethost.dll")
endif()

include(../testexe.cmake)

target_link_libraries(${DOTNET_PROJECT_NAME} nethost)

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (WIN32 AND (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64))
    target_link_libraries(${DOTNET_PROJECT_NAME} Advapi32.lib Ole32.lib OleAut32.lib)
endif()