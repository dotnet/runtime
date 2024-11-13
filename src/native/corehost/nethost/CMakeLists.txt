# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    nethost.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

add_compile_definitions(
    EXPORT_SHARED_API
    FEATURE_LIBHOST
    NETHOST_EXPORT)

add_library(nethost SHARED ${SOURCES})
add_library(libnethost STATIC ${SOURCES})

add_version_info_to_target(nethost)
add_version_info_to_target(libnethost nethost)

target_include_directories(nethost PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_include_directories(libnethost PUBLIC $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}>)

# Copy static lib PDB to the project output directory
if (WIN32)
    set_target_properties(libnethost PROPERTIES
        COMPILE_PDB_NAME "libnethost"
        COMPILE_PDB_OUTPUT_DIRECTORY "$<TARGET_FILE_DIR:libnethost>"
    )
endif(WIN32)

target_link_libraries(nethost PRIVATE hostmisc fxr_resolver)
target_link_libraries(libnethost PRIVATE hostmisc fxr_resolver)
target_compile_definitions(nethost PRIVATE FEATURE_LIBHOST NETHOST_EXPORT)
target_compile_definitions(libnethost PRIVATE FEATURE_LIBHOST NETHOST_EXPORT)

set_target_properties(nethost PROPERTIES MACOSX_RPATH TRUE)
set_target_properties(libnethost PROPERTIES MACOSX_RPATH TRUE)
set_target_properties(libnethost PROPERTIES PREFIX "")

target_link_libraries (nethost PRIVATE ${CMAKE_DL_LIBS})
target_link_libraries (libnethost PUBLIC ${CMAKE_DL_LIBS})

install(FILES ../coreclr_delegates.h DESTINATION corehost)
install(FILES ../hostfxr.h DESTINATION corehost)
install(FILES nethost.h DESTINATION corehost)
install_with_stripped_symbols(nethost TARGETS corehost)

if (MSVC)
    # We ship libnethost.lib as a static library for external consumption, so
    # LTCG must be disabled to ensure that non-MSVC toolchains can work with it.

    set_target_properties(libnethost PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)

    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELEASE ${CMAKE_STATIC_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO})
endif()

# Only Windows creates a symbols file for static libs.
if (WIN32)
    install_with_stripped_symbols(libnethost TARGETS corehost)
else()
    install(TARGETS libnethost DESTINATION corehost)
endif(WIN32)
