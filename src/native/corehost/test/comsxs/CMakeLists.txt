# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(comsxs)

set(DOTNET_PROJECT_NAME "comsxs")

set(SOURCES
    ./comsxs.cpp
    ./App.manifest
)


include(../testexe.cmake)

target_link_libraries(comsxs ole32 oleaut32)

install_with_stripped_symbols(comsxs TARGETS corehost_test)
