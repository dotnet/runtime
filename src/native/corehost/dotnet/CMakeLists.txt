# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        dotnet.manifest
        dotnet.rc)
endif()

list(APPEND SOURCES
    ../apphost/standalone/hostfxr_resolver.cpp
    ../corehost.cpp
)

add_executable(dotnet ${SOURCES})

target_link_libraries(dotnet PRIVATE hostmisc fxr_resolver)

add_sanitizer_runtime_support(dotnet)

if(NOT CLR_CMAKE_TARGET_WIN32)
    disable_pax_mprotect(dotnet)
endif()

install_with_stripped_symbols(dotnet TARGETS corehost)
add_version_info_to_target(dotnet)

if (CLR_CMAKE_HOST_APPLE)
    adhoc_sign_with_entitlements(dotnet "${CLR_ENG_NATIVE_DIR}/entitlements.plist")
endif()
