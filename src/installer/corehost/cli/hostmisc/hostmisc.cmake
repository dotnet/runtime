# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

include(${CMAKE_CURRENT_LIST_DIR}/configure.cmake)
include_directories(${CMAKE_CURRENT_BINARY_DIR})

# CMake does not recommend using globbing since it messes with the freshness checks
list(APPEND SRC_TMP
    ${CMAKE_CURRENT_LIST_DIR}/trace.cpp
    ${CMAKE_CURRENT_LIST_DIR}/utils.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../fxr/fx_ver.cpp
)

list(APPEND HDR_TMP
    ${CMAKE_CURRENT_LIST_DIR}/trace.h
    ${CMAKE_CURRENT_LIST_DIR}/utils.h
    ${CMAKE_CURRENT_LIST_DIR}/pal.h
    ${CMAKE_CURRENT_LIST_DIR}/../../error_codes.h
    ${CMAKE_CURRENT_LIST_DIR}/../fxr/fx_ver.h
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SRC_TMP
        ${CMAKE_CURRENT_LIST_DIR}/pal.windows.cpp
        ${CMAKE_CURRENT_LIST_DIR}/longfile.windows.cpp)
    list(APPEND HDR_TMP
        ${CMAKE_CURRENT_LIST_DIR}/longfile.h)
else()
    list(APPEND SRC_TMP
        ${CMAKE_CURRENT_LIST_DIR}/pal.unix.cpp)
endif()

SET(HOSTMISC_SRC "${SRC_TMP}" CACHE INTERNAL "HOSTMISC_SRC")
SET(HOSTMISC_HDR "${HDR_TMP}" CACHE INTERNAL "HOSTMISC_HDR")
