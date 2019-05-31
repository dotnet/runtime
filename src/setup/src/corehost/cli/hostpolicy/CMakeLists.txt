# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(hostpolicy)

set(DOTNET_PROJECT_NAME "hostpolicy")

# Include directories
include_directories(../fxr)
include_directories(../json/casablanca/include)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ./args.cpp
    ./breadcrumbs.cpp
    ./coreclr.cpp
    ./deps_resolver.cpp
    ./hostpolicy.cpp
    ./hostpolicy_context.cpp
    ./hostpolicy_init.cpp
    ../roll_forward_option.cpp
    ../runtime_config.cpp
    ../json/casablanca/src/json/json.cpp
    ../json/casablanca/src/json/json_parsing.cpp
    ../json/casablanca/src/json/json_serialization.cpp
    ../json/casablanca/src/utilities/asyncrt_utils.cpp
    ../fxr/fx_ver.cpp
    ../host_startup_info.cpp
    ../deps_format.cpp
    ../deps_entry.cpp
    ../fx_definition.cpp
    ../fx_reference.cpp
    ../version.cpp
    ../version_compatibility_range.cpp
)

set(HEADERS
    ./args.h
    ./breadcrumbs.h
    ./coreclr.h
    ./deps_resolver.h
    ./hostpolicy_context.h
    ./hostpolicy_init.h
    ../corehost_context_contract.h
    ../runtime_config.h
    ../json/casablanca/include/cpprest/json.h
    ../fxr/fx_ver.h
    ../host_startup_info.h
    ../deps_format.h
    ../deps_entry.h
    ../fx_definition.h
    ../fx_reference.h
    ../version.h
    ../version_compatibility_range.h
)

include(../lib.cmake)

install(TARGETS hostpolicy DESTINATION corehost)
install_symbols(hostpolicy corehost)
