# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(mockhostpolicy)

set(DOTNET_PROJECT_NAME "mockhostpolicy")

set(SOURCES
    ./mockhostpolicy.cpp
)

include(../testlib.cmake)

install_with_stripped_symbols(mockhostpolicy TARGETS corehost_test)