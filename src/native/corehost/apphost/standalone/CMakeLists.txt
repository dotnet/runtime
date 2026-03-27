# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include_directories(..)

if(CLR_CMAKE_TARGET_WIN32)
    # Windows: use existing C++ sources
    set(SOURCES
        ../bundle_marker.cpp
        ./hostfxr_resolver.cpp
        ../../corehost.cpp
    )

    set(HEADERS
        ../bundle_marker.h
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

    # Run the same configure checks that hostmisc uses for config.h
    include(CheckStructHasMember)
    include(CheckSymbolExists)
    check_struct_has_member("struct dirent" d_type dirent.h HAVE_DIRENT_D_TYPE)
    check_symbol_exists(getauxval sys/auxv.h HAVE_GETAUXVAL)
    configure_file(${CMAKE_CURRENT_SOURCE_DIR}/../../hostmisc/config.h.in ${CMAKE_CURRENT_BINARY_DIR}/config.h)

    # Configure dn-config.h for the containers library
    configure_file(${CLR_SRC_NATIVE_DIR}/containers/dn-config.h.in ${CMAKE_CURRENT_BINARY_DIR}/dn-config.h)

    set(SOURCES
        ./apphost_main.c
        ./apphost_bundle_marker.c
        ./apphost_hostfxr_resolver.c
        ./apphost_fxr_resolver.c
        ./apphost_pal.c
        ./apphost_trace.c
        ./apphost_utils.c
        ./apphost_fx_ver.c
        ${CLR_SRC_NATIVE_DIR}/containers/dn-vector.c
        ${CLR_SRC_NATIVE_DIR}/containers/dn-allocator.c
    )

    add_compile_definitions(FEATURE_APPHOST)

    add_executable(apphost ${SOURCES} ${RESOURCES})

    target_include_directories(apphost PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/..
        ${CMAKE_CURRENT_SOURCE_DIR}/../..
        ${CMAKE_CURRENT_BINARY_DIR}
        ${CMAKE_CURRENT_BINARY_DIR}/../..
        ${CLR_SRC_NATIVE_DIR}
        ${CLR_SRC_NATIVE_DIR}/containers
        ${CLR_ARTIFACTS_OBJ_DIR})

    target_link_libraries(apphost PRIVATE
        ${CMAKE_DL_LIBS}
        $<$<BOOL:${PTHREAD_LIB}>:${PTHREAD_LIB}>)

    if(CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARMV6)
        target_link_libraries(apphost PRIVATE
            $<$<BOOL:${ATOMIC_SUPPORT_LIB}>:${ATOMIC_SUPPORT_LIB}>)
    endif()

    disable_pax_mprotect(apphost)
endif()

add_sanitizer_runtime_support(apphost)

install_with_stripped_symbols(apphost TARGETS corehost)

if (CLR_CMAKE_HOST_APPLE)
    adhoc_sign_with_entitlements(apphost "${CLR_ENG_NATIVE_DIR}/entitlements.plist")
endif()
