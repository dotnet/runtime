# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(apphost)
set(DOTNET_PROJECT_NAME "apphost")

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

set(SOURCES
    ../fxr/fx_ver.cpp
)

include(../exe.cmake)

add_definitions(-DFEATURE_APPHOST=1)

install_library_and_symbols (apphost)

# Disable manifest generation into the file .exe on Windows
if(WIN32)
    set_property(TARGET ${PROJECT_NAME} PROPERTY 
            LINK_FLAGS "/MANIFEST:NO" 
        ) 
endif()