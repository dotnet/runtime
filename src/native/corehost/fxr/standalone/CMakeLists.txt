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
include(../../lib_static.cmake)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(hostfxr_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(hostfxr hostfxr_exports)

    set_property(TARGET hostfxr APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET hostfxr APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

# Copy static lib PDB to the project output directory
if (WIN32)
    set_target_properties(libhostfxr PROPERTIES
        COMPILE_PDB_NAME "libhostfxr"
        COMPILE_PDB_OUTPUT_DIRECTORY "${PROJECT_BINARY_DIR}"
    )
endif(WIN32)

install_with_stripped_symbols(hostfxr TARGETS corehost)

# Only Windows creates a symbols file for static libs.
if (WIN32)
    install_with_stripped_symbols(libhostfxr TARGETS corehost)
else()
    install(TARGETS libhostfxr DESTINATION corehost)
endif(WIN32)

target_link_libraries(hostfxr libhostcommon)
