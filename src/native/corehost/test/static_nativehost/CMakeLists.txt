# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(static_nativehost)

set(SOURCES
    ./static_nativehost.cpp
)

add_executable(static_nativehost ${SOURCES})

add_sanitizer_runtime_support(static_nativehost)

install_with_stripped_symbols(static_nativehost TARGETS corehost_test)

# Link against the static nethost library (libnethost) instead of the shared one.
# hostmisc_interface provides the include directories and transitive dependencies
# (e.g. CMAKE_DL_LIBS, pthreads, advapi32) needed at link time.
target_link_libraries(static_nativehost PRIVATE
    libnethost
    hostmisc_interface
    $<$<BOOL:${PTHREAD_LIB}>:${PTHREAD_LIB}>)

# On Windows, define NETHOST_USE_AS_STATIC so that nethost.h does not
# apply __declspec(dllimport) to the get_hostfxr_path function.
if (CLR_CMAKE_TARGET_WIN32)
    target_compile_definitions(static_nativehost PRIVATE NETHOST_USE_AS_STATIC)
endif()
