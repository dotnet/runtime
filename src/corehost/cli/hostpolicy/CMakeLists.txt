# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(hostpolicy)

set(DOTNET_PROJECT_NAME "hostpolicy")

# Include directories
include_directories(../fxr)
include_directories(../json/casablanca/include)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../../common/trace.cpp
    ../../common/utils.cpp
    ../libhost.cpp
    ../runtime_config.cpp
    ../json/casablanca/src/json/json.cpp
    ../json/casablanca/src/json/json_parsing.cpp
    ../json/casablanca/src/json/json_serialization.cpp
    ../json/casablanca/src/utilities/asyncrt_utils.cpp
    ../fxr/fx_ver.cpp
    ../breadcrumbs.cpp
    ../args.cpp
    ../hostpolicy.cpp
    ../host_startup_info.cpp
    ../coreclr.cpp
    ../deps_resolver.cpp
    ../deps_format.cpp
    ../deps_entry.cpp
    ../fx_definition.cpp
    ../fx_reference.cpp
    ../version.cpp
)

include(../lib.cmake)

install_library_and_symbols (hostpolicy)
