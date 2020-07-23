# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(hostcommon)

set(DOTNET_PROJECT_NAME "hostcommon")

# Include directories
include_directories(../fxr)
include_directories(../json)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../json_parser.cpp
    ../deps_format.cpp
    ../deps_entry.cpp
    ../host_startup_info.cpp
    ../roll_forward_option.cpp
    ../fx_definition.cpp
    ../fx_reference.cpp
    ../fxr/fx_ver.cpp
    ../version.cpp
    ../version_compatibility_range.cpp
    ../runtime_config.cpp
    ../bundle/info.cpp
    ../bundle/reader.cpp
    ../bundle/header.cpp
)

set(HEADERS
    ../json_parser.h
    ../deps_format.h
    ../deps_entry.h
    ../host_startup_info.h
    ../roll_forward_option.h
    ../fx_definition.h
    ../fx_reference.h
    ../fxr/fx_ver.h
    ../version.h
    ../version_compatibility_range.h
    ../runtime_config.h
    ../bundle/info.h
    ../bundle/reader.h
    ../bundle/header.h
)

set(SKIP_VERSIONING 1)
include(../lib_static.cmake)

