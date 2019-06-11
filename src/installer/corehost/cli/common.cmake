# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

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

list(APPEND SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/../common/trace.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../common/utils.cpp)

list(APPEND HEADERS
    ${CMAKE_CURRENT_LIST_DIR}/../common/trace.h
    ${CMAKE_CURRENT_LIST_DIR}/../common/utils.h
    ${CMAKE_CURRENT_LIST_DIR}/../common/pal.h
    ${CMAKE_CURRENT_LIST_DIR}/../error_codes.h)

if(WIN32)
    list(APPEND SOURCES 
        ${CMAKE_CURRENT_LIST_DIR}/../common/pal.windows.cpp
        ${CMAKE_CURRENT_LIST_DIR}/../common/longfile.windows.cpp)
    list(APPEND HEADERS
        ${CMAKE_CURRENT_LIST_DIR}/../common/longfile.h)
else()
    list(APPEND SOURCES
        ${CMAKE_CURRENT_LIST_DIR}/../common/pal.unix.cpp
        ${VERSION_FILE_PATH})
endif()

set(RESOURCES)
if(WIN32 AND NOT SKIP_VERSIONING)
    list(APPEND RESOURCES ${CMAKE_CURRENT_LIST_DIR}/native.rc)
endif()

if(WIN32)
    list(APPEND SOURCES ${HEADERS})
endif()

function(set_common_libs TargetType)

    # Libraries used for exe projects
    if (${TargetType} STREQUAL "exe")
        if(${CMAKE_SYSTEM_NAME} MATCHES "Linux|FreeBSD")
            target_link_libraries (${DOTNET_PROJECT_NAME} "pthread")
        endif()

        if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
            target_link_libraries (${DOTNET_PROJECT_NAME} "dl")
        endif()
    endif()

    # Specify the import library to link against for Arm32 build since the default set is minimal
    if (CLI_CMAKE_PLATFORM_ARCH_ARM)
        if (WIN32)
            target_link_libraries(${DOTNET_PROJECT_NAME} shell32.lib)
        else()
            target_link_libraries(${DOTNET_PROJECT_NAME} atomic.a)
        endif()
    endif()
endfunction()
