# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(dotnet)
set(DOTNET_PROJECT_NAME "dotnet")

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        dotnet.manifest
        dotnet.rc)
endif()

list(APPEND SOURCES
    ../apphost/standalone/hostfxr_resolver.cpp
)

include(../exe.cmake)

if (CLR_CMAKE_HOST_APPLE)
    adhoc_sign_with_entitlements(dotnet "${CLR_ENG_NATIVE_DIR}/entitlements.plist")
endif()
