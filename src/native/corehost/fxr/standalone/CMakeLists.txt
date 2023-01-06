# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(hostfxr)

set(DOTNET_PROJECT_NAME "hostfxr")

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ./hostpolicy_resolver.cpp
)

set(HEADERS
    ../hostpolicy_resolver.h
)

include(../files.cmake)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        hostfxr.def)
else(CLR_CMAKE_TARGET_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/hostfxr_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/hostfxr.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    set_exports_linker_option(${EXPORTS_FILE})
endif(CLR_CMAKE_TARGET_WIN32)

include(../../lib.cmake)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(hostfxr_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(hostfxr hostfxr_exports)

    set_property(TARGET hostfxr APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET hostfxr APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

install_with_stripped_symbols(hostfxr TARGETS corehost)

target_link_libraries(hostfxr libhostcommon)

if (CLR_CMAKE_TARGET_ARCH_ARMV6)
    target_link_libraries(${DOTNET_PROJECT_NAME} atomic)
endif()
