# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Include directories
include_directories(${CMAKE_CURRENT_LIST_DIR}/../fxr)
include_directories(${CMAKE_CURRENT_LIST_DIR}/../json)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/args.cpp
    ${CMAKE_CURRENT_LIST_DIR}/breadcrumbs.cpp
    ${CMAKE_CURRENT_LIST_DIR}/coreclr.cpp
    ${CMAKE_CURRENT_LIST_DIR}/deps_entry.cpp
    ${CMAKE_CURRENT_LIST_DIR}/deps_format.cpp
    ${CMAKE_CURRENT_LIST_DIR}/deps_resolver.cpp
    ${CMAKE_CURRENT_LIST_DIR}/hostpolicy_context.cpp
    ${CMAKE_CURRENT_LIST_DIR}/hostpolicy.cpp
    ${CMAKE_CURRENT_LIST_DIR}/hostpolicy_init.cpp
    ${CMAKE_CURRENT_LIST_DIR}/shared_store.cpp
    ${CMAKE_CURRENT_LIST_DIR}/version.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/dir_utils.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/extractor.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/file_entry.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/manifest.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/runner.cpp
)

list(APPEND HEADERS
    ${CMAKE_CURRENT_LIST_DIR}/args.h
    ${CMAKE_CURRENT_LIST_DIR}/breadcrumbs.h
    ${CMAKE_CURRENT_LIST_DIR}/coreclr.h
    ${CMAKE_CURRENT_LIST_DIR}/deps_entry.h
    ${CMAKE_CURRENT_LIST_DIR}/deps_format.h
    ${CMAKE_CURRENT_LIST_DIR}/deps_resolver.h
    ${CMAKE_CURRENT_LIST_DIR}/hostpolicy_context.h
    ${CMAKE_CURRENT_LIST_DIR}/hostpolicy_init.h
    ${CMAKE_CURRENT_LIST_DIR}/shared_store.h
    ${CMAKE_CURRENT_LIST_DIR}/version.h
    ${CMAKE_CURRENT_LIST_DIR}/../hostpolicy.h
    ${CMAKE_CURRENT_LIST_DIR}/../corehost_context_contract.h
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/dir_utils.h
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/extractor.h
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/file_entry.h
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/manifest.h
    ${CMAKE_CURRENT_LIST_DIR}/../bundle/runner.h
    ${CMAKE_CURRENT_LIST_DIR}/../coreclr_resolver.h
)
