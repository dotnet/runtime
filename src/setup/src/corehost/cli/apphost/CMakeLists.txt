# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

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
    ./bundle/file_entry.cpp
    ./bundle/manifest.cpp
    ./bundle/header.cpp
    ./bundle/bundle_runner.cpp
    ./bundle/marker.cpp
)

set(HEADERS
    ../fxr/fx_ver.h
    ./bundle/file_type.h
    ./bundle/file_entry.h
    ./bundle/manifest.h
    ./bundle/header.h
    ./bundle/bundle_runner.h
    ./bundle/marker.h
)

include(../exe.cmake)

add_definitions(-DFEATURE_APPHOST=1)

# Disable manifest generation into the file .exe on Windows
if(WIN32)
    set_property(TARGET ${PROJECT_NAME} PROPERTY 
            LINK_FLAGS "/MANIFEST:NO" 
        ) 
endif()

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (WIN32 AND (CLI_CMAKE_PLATFORM_ARCH_ARM OR CLI_CMAKE_PLATFORM_ARCH_ARM64))
    target_link_libraries(apphost Advapi32.lib)
endif()
