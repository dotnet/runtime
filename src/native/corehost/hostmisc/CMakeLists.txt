# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

include(configure.cmake)

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

# hostmisc must be an "object library" as we want to build it once
# and embed the objects into static libraries we ship (like libnethost).
add_library(hostmisc_interface INTERFACE)
target_include_directories(hostmisc_interface INTERFACE
    ${CMAKE_CURRENT_BINARY_DIR}
    ${CLR_SRC_NATIVE_DIR}
    ${CMAKE_CURRENT_LIST_DIR})

if (MSVC)
    target_link_libraries(hostmisc_interface INTERFACE advapi32)
endif()

target_link_libraries(hostmisc_interface INTERFACE
    ${CMAKE_DL_LIBS}
    $<$<BOOL:${PTHREAD_LIB}>:${PTHREAD_LIB}>)

if(CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARMV6)
    target_link_libraries(hostmisc_interface INTERFACE
    $<$<BOOL:${ATOMIC_SUPPORT_LIB}>:${ATOMIC_SUPPORT_LIB}>)
endif()


add_library(hostmisc STATIC ${SOURCES})
target_link_libraries(hostmisc PUBLIC hostmisc_interface)
if (MSVC)
    target_sources(hostmisc PRIVATE ${HEADERS})
endif()

add_library(hostmisc_public OBJECT ${SOURCES})
target_link_libraries(hostmisc_public PUBLIC hostmisc_interface)
set_target_properties(hostmisc_public PROPERTIES INTERPROCEDURAL_OPTIMIZATION OFF)

add_library(hostmisc::public ALIAS hostmisc_public)
