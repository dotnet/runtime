# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

project(${DOTNET_PROJECT_NAME})

set(SKIP_VERSIONING 1)

include(${CMAKE_CURRENT_LIST_DIR}/../common.cmake)

add_executable(${DOTNET_PROJECT_NAME} ${SOURCES})

install(TARGETS ${DOTNET_PROJECT_NAME} DESTINATION corehost_test)
install_symbols(${DOTNET_PROJECT_NAME} corehost_test)

set_common_libs("exe")