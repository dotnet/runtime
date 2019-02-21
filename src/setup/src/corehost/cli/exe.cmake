# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

project (${DOTNET_PROJECT_NAME})

include(${CMAKE_CURRENT_LIST_DIR}/common.cmake)

# Include directories
include_directories(${CMAKE_CURRENT_LIST_DIR}/fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/../corehost.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../common/trace.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../common/utils.cpp)

add_executable(${DOTNET_PROJECT_NAME} ${SOURCES} ${RESOURCES})

if(NOT WIN32)
    disable_pax_mprotect(${DOTNET_PROJECT_NAME})
endif()

install(TARGETS ${DOTNET_PROJECT_NAME} DESTINATION bin)

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux|FreeBSD")
    target_link_libraries (${DOTNET_PROJECT_NAME} "pthread")
endif()

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    target_link_libraries (${DOTNET_PROJECT_NAME} "dl")
endif()

# Specify the import library to link against for Arm32 build since the default set is minimal
if (WIN32 AND CLI_CMAKE_PLATFORM_ARCH_ARM)
    target_link_libraries(${DOTNET_PROJECT_NAME} shell32.lib)
endif()
