# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(hostpolicy_static)

set(DOTNET_PROJECT_NAME "hostpolicy_static")

# Include directories
include_directories(../../fxr)
include_directories(../../json)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ../args.cpp
    ../breadcrumbs.cpp
    ../coreclr.cpp
    ../deps_resolver.cpp
    ../hostpolicy_context.cpp
    ../hostpolicy.cpp
    ../hostpolicy_init.cpp
    ../../bundle/dir_utils.cpp
    ../../bundle/extractor.cpp
    ../../bundle/file_entry.cpp
    ../../bundle/manifest.cpp
    ../../bundle/runner.cpp
    ./coreclr_resolver.cpp
)

set(HEADERS
    ../args.h
    ../breadcrumbs.h
    ../coreclr.h
    ../deps_resolver.h
    ../hostpolicy_context.h
    ../hostpolicy_init.h
    ../../hostpolicy.h
    ../../corehost_context_contract.h
    ../../bundle/dir_utils.h
    ../../bundle/extractor.h
    ../../bundle/file_entry.h
    ../../bundle/manifest.h
    ../../bundle/runner.h
    ../../../coreclr_resolver.h
)

set(SKIP_VERSIONING 1)
set(BUILD_OBJECT_LIBRARY 1)
include(../../lib_static.cmake)

add_definitions(-DHOSTPOLICY_EMBEDDED)
