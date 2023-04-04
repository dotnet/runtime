# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Include directories
include_directories(${CMAKE_CURRENT_LIST_DIR}/../json)
include_directories(${CMAKE_CURRENT_LIST_DIR}/../fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/command_line.cpp
    ${CMAKE_CURRENT_LIST_DIR}/corehost_init.cpp
    ${CMAKE_CURRENT_LIST_DIR}/hostfxr.cpp
    ${CMAKE_CURRENT_LIST_DIR}/fx_muxer.cpp
    ${CMAKE_CURRENT_LIST_DIR}/fx_resolver.cpp
    ${CMAKE_CURRENT_LIST_DIR}/fx_resolver.messages.cpp
    ${CMAKE_CURRENT_LIST_DIR}/framework_info.cpp
    ${CMAKE_CURRENT_LIST_DIR}/host_context.cpp
    ${CMAKE_CURRENT_LIST_DIR}/install_info.cpp
    ${CMAKE_CURRENT_LIST_DIR}/sdk_info.cpp
    ${CMAKE_CURRENT_LIST_DIR}/sdk_resolver.cpp
)

list(APPEND HEADERS
    ${CMAKE_CURRENT_LIST_DIR}/../corehost_context_contract.h
    ${CMAKE_CURRENT_LIST_DIR}/../hostpolicy.h
    ${CMAKE_CURRENT_LIST_DIR}/../fx_definition.h
    ${CMAKE_CURRENT_LIST_DIR}/../fx_reference.h
    ${CMAKE_CURRENT_LIST_DIR}/../roll_fwd_on_no_candidate_fx_option.h
    ${CMAKE_CURRENT_LIST_DIR}/command_line.h
    ${CMAKE_CURRENT_LIST_DIR}/corehost_init.h
    ${CMAKE_CURRENT_LIST_DIR}/fx_muxer.h
    ${CMAKE_CURRENT_LIST_DIR}/fx_resolver.h
    ${CMAKE_CURRENT_LIST_DIR}/framework_info.h
    ${CMAKE_CURRENT_LIST_DIR}/host_context.h
    ${CMAKE_CURRENT_LIST_DIR}/install_info.h
    ${CMAKE_CURRENT_LIST_DIR}/sdk_info.h
    ${CMAKE_CURRENT_LIST_DIR}/sdk_resolver.h
)

