# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# This target builds the same sources as the nativehost test executable,
# but links against the static nethost library (libnethost) instead of
# the shared one. This validates the static library consumption scenario.

project(static_nativehost)

set(SOURCES
    ../nativehost/error_writer_redirector.cpp
    ../nativehost/get_native_search_directories_test.cpp
    ../nativehost/hostfxr_exports.cpp
    ../nativehost/hostpolicy_exports.cpp
    ../nativehost/host_context_test.cpp
    ../nativehost/resolve_component_dependencies_test.cpp
    ../nativehost/nativehost.cpp
)

set(HEADERS
    ../nativehost/error_writer_redirector.h
    ../nativehost/get_native_search_directories_test.h
    ../nativehost/hostfxr_exports.h
    ../nativehost/hostpolicy_exports.h
    ../nativehost/host_context_test.h
    ../nativehost/resolve_component_dependencies_test.h
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ../nativehost/comhost_test.cpp)

    list(APPEND HEADERS
        ../nativehost/comhost_test.h)
endif()

add_executable(static_nativehost ${SOURCES})

add_sanitizer_runtime_support(static_nativehost)

install_with_stripped_symbols(static_nativehost TARGETS corehost_test)

# Link against the static nethost library (libnethost) instead of the shared one.
target_link_libraries(static_nativehost PRIVATE
    libnethost
    hostmisc
    $<$<BOOL:${PTHREAD_LIB}>:${PTHREAD_LIB}>)

# On Windows, define NETHOST_USE_AS_STATIC so that nethost.h does not
# apply __declspec(dllimport) to the get_hostfxr_path function.
if (CLR_CMAKE_TARGET_WIN32)
    target_compile_definitions(static_nativehost PRIVATE NETHOST_USE_AS_STATIC)
    target_link_libraries(static_nativehost PRIVATE ole32 oleaut32)
endif()
