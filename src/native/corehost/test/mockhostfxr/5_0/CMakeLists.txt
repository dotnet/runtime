# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(mockhostfxr_5_0)

set(DOTNET_PROJECT_NAME "mockhostfxr_5_0")

add_definitions(-DMOCKHOSTFXR_5_0)

set(SOURCES
    ./../mockhostfxr.cpp
)

include(../../testlib.cmake)

install_with_stripped_symbols(mockhostfxr_5_0 TARGETS corehost_test)
