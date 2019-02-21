# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

project(${DOTNET_PROJECT_NAME})

if(WIN32)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:/MT>)
    add_compile_options($<$<CONFIG:Release>:/MT>)
    add_compile_options($<$<CONFIG:Debug>:/MTd>)
else()
    add_compile_options(-fPIC)
    add_compile_options(-fvisibility=hidden)
endif()

include(${CMAKE_CURRENT_LIST_DIR}/setup.cmake)

# Include directories
if(WIN32)
    include_directories("${CLI_CMAKE_RESOURCE_DIR}/${DOTNET_PROJECT_NAME}")
endif()
include_directories(${CMAKE_CURRENT_SOURCE_DIR}/)
include_directories(${CMAKE_CURRENT_LIST_DIR}/)
include_directories(${CMAKE_CURRENT_LIST_DIR}/../)
include_directories(${CMAKE_CURRENT_LIST_DIR}/../common)

if(WIN32)
    list(APPEND SOURCES 
        ${CMAKE_CURRENT_LIST_DIR}/../common/pal.windows.cpp
        ${CMAKE_CURRENT_LIST_DIR}/../common/longfile.windows.cpp)
else()
    list(APPEND SOURCES
        ${CMAKE_CURRENT_LIST_DIR}/../common/pal.unix.cpp
        ${VERSION_FILE_PATH})
endif()

set(RESOURCES)
if(WIN32 AND NOT SKIP_VERSIONING)
    list(APPEND RESOURCES ${CMAKE_CURRENT_LIST_DIR}/native.rc)
endif()

