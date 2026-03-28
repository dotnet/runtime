# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include_directories(..)

if(CLR_CMAKE_TARGET_WIN32)
    # Windows: use existing C++ sources with C bundle_marker implementation
    set(SOURCES
        ../../hostmisc/apphost_bundle_marker.c
        ./hostfxr_resolver.cpp
        ../../corehost.cpp
    )

    set(HEADERS
        ../bundle_marker.h
        ../../hostmisc/apphost_bundle_marker.h
        ../../hostfxr_resolver.h
    )

    add_compile_definitions(UNICODE)
    list(APPEND SOURCES
        ../apphost.windows.cpp)

    list(APPEND HEADERS
        ../apphost.windows.h)

    list(APPEND SOURCES ${HEADERS})

    add_compile_definitions(FEATURE_APPHOST)

    add_executable(apphost ${SOURCES} ${RESOURCES})

    target_link_libraries(apphost PRIVATE hostmisc fxr_resolver)
    target_link_libraries(apphost PRIVATE shell32)

    # Disable manifest generation into the file .exe on Windows
    target_link_options(apphost PRIVATE "/MANIFEST:NO")

    # Enable CET-compatibility
    if (CLR_CMAKE_HOST_ARCH_AMD64)
        target_link_options(apphost PRIVATE "/CETCOMPAT")
    endif()
else()
    # Non-Windows (Linux, macOS, etc.): use C sources only - no C++ in the apphost binary
    set(SOURCES
        ./apphost_main.c
        ./apphost_hostfxr_resolver.c
    )

    add_compile_definitions(FEATURE_APPHOST)

    add_executable(apphost ${SOURCES} ${RESOURCES})

    target_include_directories(apphost PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/..
        ${CMAKE_CURRENT_SOURCE_DIR}/../..)

    target_link_libraries(apphost PRIVATE hostmisc_c dn-containers m)

    disable_pax_mprotect(apphost)
endif()

add_sanitizer_runtime_support(apphost)

install_with_stripped_symbols(apphost TARGETS corehost)

if (CLR_CMAKE_HOST_APPLE)
    adhoc_sign_with_entitlements(apphost "${CLR_ENG_NATIVE_DIR}/entitlements.plist")
endif()
