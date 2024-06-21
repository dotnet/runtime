# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(nativehost)

set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
set(MACOSX_RPATH ON)
if (CLR_CMAKE_TARGET_OSX)
    set(CMAKE_INSTALL_RPATH "@loader_path")
else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
endif()

set(SOURCES
    ./error_writer_redirector.cpp
    ./get_native_search_directories_test.cpp
    ./hostfxr_exports.cpp
    ./hostpolicy_exports.cpp
    ./host_context_test.cpp
    ./resolve_component_dependencies_test.cpp
    ./nativehost.cpp
)

set(HEADERS
    ./error_writer_redirector.h
    ./get_native_search_directories_test.h
    ./hostfxr_exports.h
    ./hostpolicy_exports.h
    ./host_context_test.h
    ./resolve_component_dependencies_test.h
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ./comhost_test.cpp)

    list(APPEND HEADERS
        ./comhost_test.h)
endif()

add_executable(nativehost ${SOURCES})

add_sanitizer_runtime_support(nativehost)

install_with_stripped_symbols(nativehost TARGETS corehost_test)

target_link_libraries(nativehost PRIVATE
    $<$<BOOL:${PTHREAD_LIB}>:${PTHREAD_LIB}>)


target_link_libraries(nativehost PRIVATE nethost hostmisc)

if (CLR_CMAKE_TARGET_WIN32)
    target_link_options(nativehost PRIVATE /DELAYLOAD:$<TARGET_FILE_NAME:nethost>)
    target_link_libraries(nativehost PRIVATE delayimp.lib ole32 oleaut32)
endif()

