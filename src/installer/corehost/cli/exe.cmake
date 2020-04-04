# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project (${DOTNET_PROJECT_NAME})

cmake_policy(SET CMP0011 NEW)
cmake_policy(SET CMP0083 NEW)

include(${CMAKE_CURRENT_LIST_DIR}/common.cmake)

# Include directories
include_directories(${CMAKE_CURRENT_LIST_DIR}/fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/fxr_resolver.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../corehost.cpp
)

add_executable(${DOTNET_PROJECT_NAME} ${SOURCES} ${RESOURCES})
target_link_libraries(${DOTNET_PROJECT_NAME} libhostmisc)

if(NOT CLR_CMAKE_TARGET_WIN32)
    disable_pax_mprotect(${DOTNET_PROJECT_NAME})
endif()

install_with_stripped_symbols(${DOTNET_PROJECT_NAME} TARGETS corehost)

set_common_libs("exe")
