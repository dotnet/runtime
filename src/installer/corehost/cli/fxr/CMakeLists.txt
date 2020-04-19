# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(hostfxr)

set(DOTNET_PROJECT_NAME "hostfxr")

# Include directories
include_directories(../json)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ./command_line.cpp
    ./corehost_init.cpp
    ./hostfxr.cpp
    ./fx_muxer.cpp
    ./fx_resolver.cpp
    ./fx_resolver.messages.cpp
    ./framework_info.cpp
    ./host_context.cpp
    ./hostpolicy_resolver.cpp
    ./sdk_info.cpp
    ./sdk_resolver.cpp
)

set(HEADERS
    ../corehost_context_contract.h
    ../hostpolicy.h
    ../fx_definition.h
    ../fx_reference.h
    ../roll_fwd_on_no_candidate_fx_option.h
    ./command_line.h
    ./corehost_init.h
    ./fx_muxer.h
    ./fx_resolver.h
    ./framework_info.h
    ./host_context.h
    ./hostpolicy_resolver.h
    ./sdk_info.h
    ./sdk_resolver.h
)

include(../lib.cmake)

install_with_stripped_symbols(hostfxr TARGETS corehost)
target_link_libraries(hostfxr libhostcommon)
