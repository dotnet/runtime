# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(${DOTNET_PROJECT_NAME})

set(SKIP_VERSIONING 1)

include(${CMAKE_CURRENT_LIST_DIR}/../common.cmake)

add_executable(${DOTNET_PROJECT_NAME} ${SOURCES})

install_with_stripped_symbols(${DOTNET_PROJECT_NAME} TARGETS corehost_test)

set_common_libs("exe")