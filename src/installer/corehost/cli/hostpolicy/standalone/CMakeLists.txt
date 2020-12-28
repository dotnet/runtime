# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(hostpolicy)

set(DOTNET_PROJECT_NAME "hostpolicy")

set(SOURCES
    ./coreclr_resolver.cpp
)

include(../files.cmake)

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

# The cmake install generated above creates a cmake_install.cmake file
# which runs install_name_tool on osx and therefore modifies the library
# this corrupts the signature
# Repair the signature on osx
install_adhoc_codesign(hostpolicy "${CMAKE_INSTALL_PREFIX}/corehost")
