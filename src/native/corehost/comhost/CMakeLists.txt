# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(comhost)

set(DOTNET_PROJECT_NAME "comhost")

# Include directories
include_directories(../fxr)
include_directories(../json)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    comhost.cpp
    ../fxr_resolver.cpp
    clsidmap.cpp
    ../redirected_error_writer.cpp
)

set(HEADERS
    comhost.h
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

include(../lib.cmake)

add_definitions(-DFEATURE_LIBHOST=1)

if (CLR_CMAKE_TARGET_WIN32)
    set(WINLIBS wintrust.lib)

    # Specify non-default Windows libs to be used for Arm/Arm64 builds
    if (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)
        list(APPEND WINLIBS Advapi32.lib Ole32.lib OleAut32.lib)
    endif()

    target_link_libraries(comhost ${WINLIBS})
endif()

install_with_stripped_symbols(comhost TARGETS corehost)
target_link_libraries(comhost libhostcommon)
