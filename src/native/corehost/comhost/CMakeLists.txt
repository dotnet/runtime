# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Include directories
include_directories(../json)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    comhost.cpp
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

add_compile_definitions(FEATURE_LIBHOST)
add_compile_definitions(EXPORT_SHARED_API)

add_library(comhost SHARED ${SOURCES})

add_version_info_to_target(comhost)

if (CLR_CMAKE_TARGET_WIN32)
    set(WINLIBS wintrust.lib)

    # Specify non-default Windows libs to be used for Arm/Arm64 builds
    if (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)
        list(APPEND WINLIBS Ole32.lib OleAut32.lib)
    endif()

    target_link_libraries(comhost PRIVATE ${WINLIBS})
endif()

install_with_stripped_symbols(comhost TARGETS corehost)
target_link_libraries(comhost PRIVATE libhostcommon fxr_resolver)
