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

# The cmake install generated above creates a cmake_install.cmake file
# which runs install_name_tool on osx and therefore modifies the library
# this corrupts the signature
# Repair the signature on osx
install_adhoc_codesign(mockcoreclr "${CMAKE_INSTALL_PREFIX}/corehost_test")
