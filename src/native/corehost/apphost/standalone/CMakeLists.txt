# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include_directories(..)

set(SOURCES
    ../apphost_main.c
    ./apphost_hostfxr_resolver.c
)

set(HEADERS
    ../../hostmisc/bundle_marker.h
    ../../hostmisc/pal.h
    ../apphost_hostfxr_resolver.h
)

add_compile_definitions(FEATURE_APPHOST)

if(CLR_CMAKE_TARGET_WIN32)
    # fxr_resolver.c uses dn-containers (dn_vector_t), build it here alongside the containers library
    add_subdirectory(${CLR_SRC_NATIVE_DIR}/containers containers)

    list(APPEND SOURCES
        ../../hostmisc/fxr_resolver.c
        ../apphost.windows.cpp)
    list(APPEND HEADERS
        ../apphost.windows.h)

    add_compile_definitions(UNICODE)
else()
    # On non-Windows, fxr_resolver.c is compiled as part of hostmisc_c
endif()

list(APPEND SOURCES ${HEADERS})

add_executable(apphost ${SOURCES} ${RESOURCES})

target_include_directories(apphost PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/..
    ${CMAKE_CURRENT_SOURCE_DIR}/../..)

target_link_libraries(apphost PRIVATE hostmisc_c)

if(CLR_CMAKE_TARGET_WIN32)
    target_link_libraries(apphost PRIVATE dn-containers shell32)

    # Disable manifest generation into the file .exe on Windows
    target_link_options(apphost PRIVATE "/MANIFEST:NO")

    # Enable CET-compatibility
    if (CLR_CMAKE_HOST_ARCH_AMD64)
        target_link_options(apphost PRIVATE "/CETCOMPAT")
    endif()
else()
    target_link_libraries(apphost PRIVATE dn-containers m)
    disable_pax_mprotect(apphost)
endif()

add_sanitizer_runtime_support(apphost)

install_with_stripped_symbols(apphost TARGETS corehost)

if (CLR_CMAKE_HOST_APPLE)
    adhoc_sign_with_entitlements(apphost "${CLR_ENG_NATIVE_DIR}/entitlements.plist")
endif()
