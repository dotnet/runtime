# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include(configure.cmake)

add_library(hostmisc INTERFACE)

target_include_directories(hostmisc INTERFACE
    ${CMAKE_CURRENT_BINARY_DIR}
    ${CLR_SRC_NATIVE_DIR}
    ${CMAKE_CURRENT_LIST_DIR})

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    ${CMAKE_CURRENT_LIST_DIR}/trace.cpp
    ${CMAKE_CURRENT_LIST_DIR}/utils.cpp
    ${CMAKE_CURRENT_LIST_DIR}/../fxr/fx_ver.cpp
)

set(HEADERS
    ${CMAKE_CURRENT_LIST_DIR}/trace.h
    ${CMAKE_CURRENT_LIST_DIR}/utils.h
    ${CMAKE_CURRENT_LIST_DIR}/pal.h
    ${CMAKE_CURRENT_LIST_DIR}/../error_codes.h
    ${CMAKE_CURRENT_LIST_DIR}/../fxr/fx_ver.h
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ${CMAKE_CURRENT_LIST_DIR}/pal.windows.cpp
        ${CMAKE_CURRENT_LIST_DIR}/longfile.windows.cpp)
    list(APPEND HEADERS
        ${CMAKE_CURRENT_LIST_DIR}/longfile.h)
else()
    list(APPEND SOURCES
        ${CMAKE_CURRENT_LIST_DIR}/pal.unix.cpp)
endif()

target_sources(hostmisc INTERFACE ${SOURCES})

if (MSVC)
    target_sources(hostmisc INTERFACE ${HEADERS})
    target_link_libraries(hostmisc INTERFACE advapi32)
endif()

if (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARMV6 AND NOT CLR_CMAKE_TARGET_WIN32)
    target_link_libraries(hostmisc INTERFACE atomic)
endif()

target_link_libraries (hostmisc INTERFACE ${CMAKE_DL_LIBS})

if((CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD) AND NOT CLR_CMAKE_TARGET_ANDROID)
    target_link_libraries (hostmisc INTERFACE pthread)
endif()

target_compile_definitions(hostmisc INTERFACE
    _NO_ASYNCRTIMP
    _NO_PPLXIMP)