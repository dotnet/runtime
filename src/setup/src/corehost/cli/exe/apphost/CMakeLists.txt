# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
set(DOTNET_HOST_EXE_NAME "apphost")

# Add RPATH to the apphost binary that allows using local copies of shared libraries
# dotnet core depends on for special scenarios when system wide installation of such 
# dependencies is not possible for some reason.
# This cannot be enabled for MacOS (Darwin) since its RPATH works in a different way,
# doesn't apply to libraries loaded via dlopen and most importantly, it is not transitive.
if (NOT CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "\$ORIGIN/netcoredeps")
endif()

set(SKIP_VERSIONING 1)
include(../exe.cmake)
set(SOURCES)
add_definitions(-DFEATURE_APPHOST=1)

