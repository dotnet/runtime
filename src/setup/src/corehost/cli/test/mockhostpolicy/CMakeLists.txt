# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

cmake_minimum_required (VERSION 2.6)
project(mockhostpolicy)

set(DOTNET_PROJECT_NAME "mockhostpolicy")

set(SOURCES
    ./mockhostpolicy.cpp
)

include(../testlib.cmake)

install(TARGETS mockhostpolicy DESTINATION corehost_test)
install_symbols(mockhostpolicy corehost_test)