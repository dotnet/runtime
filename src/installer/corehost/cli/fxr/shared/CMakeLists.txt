# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(hostfxr)

set(DOTNET_PROJECT_NAME "hostfxr")

# Include directories
include_directories(../../json)
include_directories(../static)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ./hostpolicy_resolver.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        Exports.def)
endif()

set(HEADERS
    ../static/command_line.h
    ../static/corehost_init.h
    ../static/fx_muxer.h
    ../static/fx_resolver.h
    ../static/framework_info.h
    ../static/host_context.h
    ../static/hostpolicy_resolver.h
    ../static/sdk_info.h
    ../static/sdk_resolver.h
)

include(../../lib.cmake)

install_with_stripped_symbols(hostfxr TARGETS corehost)
target_link_libraries(hostfxr libhostcommon libhostfxr_static)
