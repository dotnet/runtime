# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include_directories(..)

set(SOURCES
    ../apphost.c
    ../bundle_marker.c
    ./apphost_hostfxr_resolver.c
)

set(HEADERS
    ../bundle_marker.h
    ../../hostmisc/pal.h
    ../apphost_hostfxr_resolver.h
)

add_compile_definitions(FEATURE_APPHOST)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ../apphost.windows.cpp)
    list(APPEND HEADERS
        ../apphost.windows.h)

    add_compile_definitions(UNICODE)
endif()

list(APPEND SOURCES ${HEADERS})

add_executable(apphost ${SOURCES} ${RESOURCES})

target_link_libraries(apphost PRIVATE fxr_resolver)

if(CLR_CMAKE_TARGET_WIN32)
    target_link_libraries(apphost PRIVATE hostmisc shell32)

    # Disable manifest generation into the file .exe on Windows
    target_link_options(apphost PRIVATE "/MANIFEST:NO")

    # Enable CET-compatibility
    if (CLR_CMAKE_HOST_ARCH_AMD64)
        target_link_options(apphost PRIVATE "/CETCOMPAT")
    endif()
else()
    target_link_libraries(apphost PRIVATE hostmisc_c minipal_objects m)
    disable_pax_mprotect(apphost)
endif()

add_sanitizer_runtime_support(apphost)

install_with_stripped_symbols(apphost TARGETS corehost)

if (CLR_CMAKE_HOST_APPLE)
    adhoc_sign_with_entitlements(apphost "${CLR_ENG_NATIVE_DIR}/entitlements.plist")
endif()
