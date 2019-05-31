# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(hostfxr)

set(DOTNET_PROJECT_NAME "hostfxr")

# Include directories
include_directories(../json/casablanca/include)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../deps_format.cpp
    ../deps_entry.cpp
    ../host_startup_info.cpp
    ../roll_forward_option.cpp
    ../runtime_config.cpp
    ../json/casablanca/src/json/json.cpp
    ../json/casablanca/src/json/json_parsing.cpp
    ../json/casablanca/src/json/json_serialization.cpp
    ../json/casablanca/src/utilities/asyncrt_utils.cpp
    ../fx_definition.cpp
    ../fx_reference.cpp
    ../version.cpp
    ../version_compatibility_range.cpp
    ./command_line.cpp
    ./corehost_init.cpp
    ./hostfxr.cpp
    ./fx_ver.cpp
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
    ../deps_format.h
    ../deps_entry.h
    ../host_startup_info.h
    ../runtime_config.h
    ../json/casablanca/include/cpprest/json.h
    ../fx_definition.h
    ../fx_reference.h
    ../roll_forward_option.h
    ../roll_fwd_on_no_candidate_fx_option.h
    ../version.h
    ../version_compatibility_range.h
    ./command_line.h
    ./corehost_init.h
    ./fx_ver.h
    ./fx_muxer.h
    ./fx_resolver.h
    ./framework_info.h
    ./host_context.h
    ./hostpolicy_resolver.h
    ./sdk_info.h
    ./sdk_resolver.h
)

include(../lib.cmake)

install(TARGETS hostfxr DESTINATION corehost)
install_symbols(hostfxr corehost)
