# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

project(test_fx_ver)

set(EXE_NAME "test_fx_ver")

include_directories(../../)
include_directories(../)
include_directories(../fxr)
include_directories(../hostmisc)

set(SOURCES
    test_fx_ver.cpp
)

include(${CMAKE_CURRENT_LIST_DIR}/../hostmisc/hostmisc.cmake)

if(CLR_CMAKE_HOST_WIN32)
    add_compile_options($<$<CONFIG:RelWithDebInfo>:/MT>)
    add_compile_options($<$<CONFIG:Release>:/MT>)
    add_compile_options($<$<CONFIG:Debug>:/MTd>)
else()
    add_compile_options(-fvisibility=hidden)
endif()

add_executable(${EXE_NAME} ${SOURCES})
target_link_libraries(${EXE_NAME} libhostcommon)

install(TARGETS ${EXE_NAME} DESTINATION corehost_test)

if(CLR_CMAKE_TARGET_LINUX)
    target_link_libraries (${EXE_NAME} "dl")
endif()

if(CLR_CMAKE_TARGET_LINUX AND CLR_CMAKE_TARGET_ARCH_ARM)
    target_link_libraries (${EXE_NAME} "atomic")
endif()
