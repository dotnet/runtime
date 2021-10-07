# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(mockhostfxr_2_2)

set(DOTNET_PROJECT_NAME "mockhostfxr_2_2")

add_definitions(-D_MOCKHOSTFXR_2_2)

set(SOURCES
    ./../mockhostfxr.cpp
)

include(../../testlib.cmake)

install_with_stripped_symbols(mockhostfxr_2_2 TARGETS corehost_test)
