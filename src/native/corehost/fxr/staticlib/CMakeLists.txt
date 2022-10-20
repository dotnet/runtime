# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(hostfxr)

set(DOTNET_PROJECT_NAME "hostfxr")

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../standalone/hostpolicy_resolver.cpp
)

set(HEADERS
    ../hostpolicy_resolver.h
)

include(../../hostcommon/files.cmake)
include(../files.cmake)

include(../../hostmisc/hostmisc.cmake)
include(../../lib_static.cmake)

# Copy static lib PDB to the project output directory
if (WIN32)
    set_target_properties(libhostfxr PROPERTIES
        COMPILE_PDB_NAME "libhostfxr"
        COMPILE_PDB_OUTPUT_DIRECTORY "${PROJECT_BINARY_DIR}"
    )
endif(WIN32)

if (MSVC)
    # We ship libhostfxr.lib as a static library for external consumption, so
    # LTCG must be disabled to ensure that non-MSVC toolchains can work with it.

    target_compile_options(libhostfxr PRIVATE /GL-)

    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELEASE ${CMAKE_STATIC_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO})
endif()

# Only Windows creates a symbols file for static libs.
if (WIN32)
    install_with_stripped_symbols(libhostfxr TARGETS corehost)
else()
    install(TARGETS libhostfxr DESTINATION corehost)
endif(WIN32)
