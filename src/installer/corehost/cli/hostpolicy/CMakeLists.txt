# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(hostpolicy)

set(DOTNET_PROJECT_NAME "hostpolicy")

# Include directories
include_directories(../fxr)
include_directories(../json)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ./args.cpp
    ./breadcrumbs.cpp
    ./coreclr.cpp
    ./deps_resolver.cpp
    ./hostpolicy_context.cpp
    ./hostpolicy.cpp
    ./hostpolicy_init.cpp
    ../bundle/dir_utils.cpp
    ../bundle/extractor.cpp
    ../bundle/file_entry.cpp
    ../bundle/manifest.cpp
    ../bundle/runner.cpp
)

set(HEADERS
    ./args.h
    ./breadcrumbs.h
    ./coreclr.h
    ../corehost_context_contract.h
    ./deps_resolver.h
    ./hostpolicy_context.h
    ../hostpolicy.h
    ./hostpolicy_init.h
    ../bundle/dir_utils.h
    ../bundle/extractor.h
    ../bundle/file_entry.h
    ../bundle/manifest.h
    ../bundle/runner.h
)

include(../lib.cmake)

install_with_stripped_symbols(hostpolicy TARGETS corehost)
target_link_libraries(hostpolicy libhostcommon)
