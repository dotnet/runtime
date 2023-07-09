# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(apphost)
set(DOTNET_PROJECT_NAME "apphost")

# Add RPATH to the apphost binary that allows using local copies of shared libraries
# dotnet core depends on for special scenarios when system wide installation of such
# dependencies is not possible for some reason.
# This cannot be enabled for MacOS (Darwin) since its RPATH works in a different way,
# doesn't apply to libraries loaded via dlopen and most importantly, it is not transitive.
if (NOT CLR_CMAKE_TARGET_OSX)
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "\$ORIGIN/netcoredeps")
endif()

set(SKIP_VERSIONING 1)

include_directories(..)

set(SOURCES
    ../bundle_marker.cpp
    ./hostfxr_resolver.cpp
)

set(HEADERS
    ../bundle_marker.h
    ../../hostfxr_resolver.h
)

if(CLR_CMAKE_TARGET_WIN32)
    add_definitions(-DUNICODE)
    list(APPEND SOURCES
        ../apphost.windows.cpp)

    list(APPEND HEADERS
        ../apphost.windows.h)
endif()

include(../../exe.cmake)

add_definitions(-DFEATURE_APPHOST=1)

# Disable manifest generation into the file .exe on Windows
if(CLR_CMAKE_TARGET_WIN32)
    set_property(TARGET ${PROJECT_NAME} PROPERTY
            LINK_FLAGS "/MANIFEST:NO"
        )
endif()

# Specify non-default Windows libs to be used for Arm64 builds
if (CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_ARM64)
    target_link_libraries(apphost PRIVATE shell32.lib)
endif()
