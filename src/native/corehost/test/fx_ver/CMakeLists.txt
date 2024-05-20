# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

project(test_fx_ver)

set(DOTNET_PROJECT_NAME "test_fx_ver")

include_directories(../../fxr)

set(SOURCES
    test_fx_ver.cpp
)

include(../testexe.cmake)

target_link_libraries(${DOTNET_PROJECT_NAME} PRIVATE libhostcommon hostmisc)
