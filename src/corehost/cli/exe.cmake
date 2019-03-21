# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project (${DOTNET_PROJECT_NAME})

include(${CMAKE_CURRENT_LIST_DIR}/common.cmake)

# Include directories
include_directories(${CMAKE_CURRENT_LIST_DIR}/fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/fxr_resolver.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../corehost.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../common/trace.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../common/utils.cpp)

add_executable(${DOTNET_PROJECT_NAME} ${SOURCES} ${RESOURCES})

if(NOT WIN32)
    disable_pax_mprotect(${DOTNET_PROJECT_NAME})
endif()

install(TARGETS ${DOTNET_PROJECT_NAME} DESTINATION corehost)
install_symbols(${DOTNET_PROJECT_NAME} corehost)

set_common_libs("exe")
