# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

cmake_minimum_required (VERSION 2.6)
project(mockhostfxr)

set(DOTNET_PROJECT_NAME "mockhostfxr")

set(SOURCES
    ./mockhostfxr.cpp
)

include(../testlib.cmake)

install(TARGETS mockhostfxr DESTINATION corehost_test)
install_symbols(mockhostfxr corehost_test)
