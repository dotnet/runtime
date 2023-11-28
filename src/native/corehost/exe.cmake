# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project (${DOTNET_PROJECT_NAME})

include(${CMAKE_CURRENT_LIST_DIR}/common.cmake)
include(${CMAKE_CURRENT_LIST_DIR}/hostmisc/hostmisc.cmake)

# Include directories
include_directories(${CMAKE_CURRENT_LIST_DIR}/fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/fxr_resolver.cpp
    ${CMAKE_CURRENT_LIST_DIR}/corehost.cpp
)
list(APPEND HEADERS
    ${CMAKE_CURRENT_LIST_DIR}/hostfxr_resolver.h
)

add_executable(${DOTNET_PROJECT_NAME} ${SOURCES} ${RESOURCES})

add_sanitizer_runtime_support(${DOTNET_PROJECT_NAME})

if(NOT CLR_CMAKE_TARGET_WIN32)
    disable_pax_mprotect(${DOTNET_PROJECT_NAME})
endif()

install_with_stripped_symbols(${DOTNET_PROJECT_NAME} TARGETS corehost ${ADDITIONAL_INSTALL_ARGUMENTS})

set_common_libs("exe")
