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

include(../../lib.cmake)

install_with_stripped_symbols(hostpolicy TARGETS corehost)
target_link_libraries(hostpolicy libhostcommon libhostpolicy_static)
