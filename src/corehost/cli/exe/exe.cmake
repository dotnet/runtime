# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
project (${DOTNET_HOST_EXE_NAME})

if(WIN32)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:/MT>)
    add_compile_options($<$<CONFIG:Release>:/MT>)
    add_compile_options($<$<CONFIG:Debug>:/MTd>)
else()
    add_compile_options(-fPIE)
endif()

include(../../setup.cmake)

# Include directories
if(WIN32)
    include_directories("${CLI_CMAKE_RESOURCE_DIR}/${DOTNET_HOST_EXE_NAME}")
endif()
include_directories(../../../)
include_directories(../../../common)
include_directories(../../)
include_directories(../../fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ../../../corehost.cpp
    ../../../common/trace.cpp
    ../../../common/utils.cpp)

if(WIN32)
    list(APPEND SOURCES ../../../common/pal.windows.cpp)
else()
    list(APPEND SOURCES ../../../common/pal.unix.cpp)
endif()

set(RESOURCES)
if(WIN32)
    list(APPEND RESOURCES ../../native.rc)
endif()

add_executable(${DOTNET_HOST_EXE_NAME} ${SOURCES} ${RESOURCES})
install(TARGETS ${DOTNET_HOST_EXE_NAME} DESTINATION bin)

# Specify the import library to link against for Arm32 build since the default set is minimal
if (WIN32 AND CLI_CMAKE_PLATFORM_ARCH_ARM)
    target_link_libraries(${DOTNET_HOST_EXE_NAME} shell32.lib)
endif()

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    target_link_libraries (${DOTNET_HOST_EXE_NAME} "dl" "pthread")
endif()


