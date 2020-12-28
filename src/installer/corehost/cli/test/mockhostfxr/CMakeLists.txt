# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(mockhostfxr_2_2)

set(DOTNET_PROJECT_NAME "mockhostfxr_2_2")

set(SOURCES
    ./mockhostfxr.cpp
)

include(../testlib.cmake)

install_with_stripped_symbols(mockhostfxr_2_2 TARGETS corehost_test)

# The cmake install generated above creates a cmake_install.cmake file
# which runs install_name_tool on osx and therefore modifies the library
# this corrupts the signature
# Repair the signature on osx
install_adhoc_codesign(mockhostfxr_2_2 "${CMAKE_INSTALL_PREFIX}/corehost_test")
