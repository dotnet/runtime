# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(mockcoreclr)

set(DOTNET_PROJECT_NAME "mockcoreclr")

set(SOURCES
    ./mockcoreclr.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        mockcoreclr.def)
endif()

include(../testlib.cmake)

install_with_stripped_symbols(mockcoreclr TARGETS corehost_test)
