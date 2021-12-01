# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(lib${DOTNET_PROJECT_NAME})

include(${CMAKE_CURRENT_LIST_DIR}/common.cmake)

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
add_definitions(-DEXPORT_SHARED_API=1)

if (BUILD_OBJECT_LIBRARY)
    add_library(lib${DOTNET_PROJECT_NAME} OBJECT ${SOURCES} ${RESOURCES})
else ()
    add_library(lib${DOTNET_PROJECT_NAME} STATIC ${SOURCES} ${RESOURCES})
endif ()

set_target_properties(lib${DOTNET_PROJECT_NAME} PROPERTIES MACOSX_RPATH TRUE)
set_target_properties(lib${DOTNET_PROJECT_NAME} PROPERTIES PREFIX "")

set_common_libs("lib-static")
