# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

project(${DOTNET_PROJECT_NAME})

include(${CMAKE_CURRENT_LIST_DIR}/common.cmake)

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
add_definitions(-DEXPORT_SHARED_API=1)

add_library(${DOTNET_PROJECT_NAME} SHARED ${SOURCES} ${RESOURCES})

set_target_properties(${DOTNET_PROJECT_NAME} PROPERTIES MACOSX_RPATH TRUE)

set_common_libs("lib")
