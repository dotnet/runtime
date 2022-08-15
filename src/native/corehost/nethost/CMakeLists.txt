# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(nethost)

set(DOTNET_PROJECT_NAME "nethost")

# Include directories
include_directories(../fxr)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    nethost.cpp
    ../fxr_resolver.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

include(../lib.cmake)
include(../lib_static.cmake)

add_definitions(-DFEATURE_LIBHOST=1)
add_definitions(-DNETHOST_EXPORT)

# Copy static lib PDB to the project output directory
if (WIN32)
    set_target_properties(libnethost PROPERTIES
        COMPILE_PDB_NAME "libnethost"
        COMPILE_PDB_OUTPUT_DIRECTORY "${PROJECT_BINARY_DIR}"
    )
endif(WIN32)

install(FILES ../coreclr_delegates.h DESTINATION corehost)
install(FILES ../hostfxr.h DESTINATION corehost)
install(FILES nethost.h DESTINATION corehost)
install_with_stripped_symbols(nethost TARGETS corehost)

if (MSVC)
    # We ship libnethost.lib as a static library for external consumption, so
    # LTCG must be disabled to ensure that non-MSVC toolchains can work with it.

    target_compile_options(libnethost PRIVATE /GL-)

    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELEASE ${CMAKE_STATIC_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO})
endif()

# Only Windows creates a symbols file for static libs.
if (WIN32)
    install_with_stripped_symbols(libnethost TARGETS corehost)
else()
    install(TARGETS libnethost DESTINATION corehost)
endif(WIN32)
