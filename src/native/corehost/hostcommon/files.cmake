# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Include directories
include_directories(${CMAKE_CURRENT_LIST_DIR}/../fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/../json_parser.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../host_startup_info.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../roll_forward_option.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../fx_definition.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../fx_reference.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../fxr/fx_ver.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../version_compatibility_range.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../runtime_config.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/info.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/reader.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/header.cpp
)

list(APPEND HEADERS
    ${CMAKE_CURRENT_LIST_DIR}/../json_parser.h
    ${CMAKE_CURRENT_LIST_DIR}/../host_startup_info.h
    ${CMAKE_CURRENT_LIST_DIR}/../roll_forward_option.h
    ${CMAKE_CURRENT_LIST_DIR}/../fx_definition.h
    ${CMAKE_CURRENT_LIST_DIR}/../fx_reference.h
    ${CMAKE_CURRENT_LIST_DIR}/../fxr/fx_ver.h
    ${CMAKE_CURRENT_LIST_DIR}/../version_compatibility_range.h
    ${CMAKE_CURRENT_LIST_DIR}/../runtime_config.h
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/info.h
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/reader.h
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/header.h
)
