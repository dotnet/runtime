# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(hostpolicy)

set(DOTNET_PROJECT_NAME "hostpolicy")

# CMake does not recommend using globbing since it messes with the freshness checks

# Can't call add_library() without source files. Create an empty .c file,
# then link with the static library just recently built.
if (NOT EXISTS "${CMAKE_CURRENT_BINARY_DIR}/empty.cpp")
	file(WRITE "${CMAKE_CURRENT_BINARY_DIR}/empty.cpp" "")
endif ()

set(SOURCES
    ${CMAKE_CURRENT_BINARY_DIR}/empty.cpp
)

set(HEADERS
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        hostpolicy.def)
else(CLR_CMAKE_TARGET_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/hostpolicy_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/hostpolicy.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD)
        # Add linker exports file option
        set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
    endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD)

    if(CLR_CMAKE_HOST_OSX)
        # Add linker exports file option
        set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
    endif(CLR_CMAKE_HOST_OSX)

    if(CLR_CMAKE_HOST_SUNOS)
        # Add linker exports file option
        set(EXPORTS_LINKER_OPTION -Wl,-M,${EXPORTS_FILE})
    endif(CLR_CMAKE_HOST_SUNOS)
endif(CLR_CMAKE_TARGET_WIN32)

include(../../lib.cmake)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(hostpolicy_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(hostpolicy hostpolicy_exports)

    set_property(TARGET hostpolicy APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET hostpolicy APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

install_with_stripped_symbols(hostpolicy TARGETS corehost)
target_link_libraries(hostpolicy libhostcommon libhostpolicy_static)
