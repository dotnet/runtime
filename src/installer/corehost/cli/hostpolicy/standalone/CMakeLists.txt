# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(hostpolicy)

set(DOTNET_PROJECT_NAME "hostpolicy")

# Include directories
include_directories(../../fxr)
include_directories(../../json)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../args.cpp
    ../breadcrumbs.cpp
    ../coreclr.cpp
    ../deps_resolver.cpp
    ../hostpolicy_context.cpp
    ../hostpolicy.cpp
    ../hostpolicy_init.cpp
    ../../bundle/dir_utils.cpp
    ../../bundle/extractor.cpp
    ../../bundle/file_entry.cpp
    ../../bundle/manifest.cpp
    ../../bundle/runner.cpp
    ./coreclr_resolver.cpp
)

set(HEADERS
    ../args.h
    ../breadcrumbs.h
    ../coreclr.h
    ../deps_resolver.h
    ../hostpolicy_context.h
    ../hostpolicy_init.h
    ../../hostpolicy.h
    ../../corehost_context_contract.h
    ../../bundle/dir_utils.h
    ../../bundle/extractor.h
    ../../bundle/file_entry.h
    ../../bundle/manifest.h
    ../../bundle/runner.h
    ../../../coreclr_resolver.h
)


if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        hostpolicy.def)
else(CLR_CMAKE_TARGET_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/hostpolicy_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/hostpolicy.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    set_exports_linker_option(${EXPORTS_FILE})
endif(CLR_CMAKE_TARGET_WIN32)

include(../../lib.cmake)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(hostpolicy_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(hostpolicy hostpolicy_exports)

    set_property(TARGET hostpolicy APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET hostpolicy APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

install_with_stripped_symbols(hostpolicy TARGETS corehost)
target_link_libraries(hostpolicy libhostcommon)
