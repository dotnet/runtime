# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(mockhostfxr_2_2)

set(DOTNET_PROJECT_NAME "mockhostfxr_2_2")

set(SOURCES
    ./mockhostfxr.cpp
)

include(../testlib.cmake)

install_with_stripped_symbols(mockhostfxr_2_2 TARGETS corehost_test)
