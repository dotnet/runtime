cmake_minimum_required(VERSION 3.20)
project(dnmd
    LANGUAGES C)

# Always include our public headers
include_directories(src/inc)

# Include the metadata API headers provided by CoreCLR
include_directories(${CMAKE_CURRENT_SOURCE_DIR}/../../coreclr/inc ${CMAKE_CURRENT_SOURCE_DIR}/../../coreclr/pal/prebuilt/inc)

if ("${CMAKE_CURRENT_SOURCE_DIR}" STREQUAL "${CMAKE_SOURCE_DIR}")
    # This is our root CMakeList.txt, so we need to set up some global settings and include the minipal here.
    include(../../../eng/native/configurepaths.cmake)
    include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

    add_subdirectory(${CLR_SRC_NATIVE_DIR}/minipal minipal)
endif()

include_directories(${CLR_SRC_NATIVE_DIR})

option(DNMD_BUILD_TESTS "Build tests for dnmd" ON)
option(DNMD_INSTALL "Install dnmd targets" ON)
option(DNMD_BUILD_INTERFACES "Build implementations of the metadata interfaces for DNMD" ON)
option(DNMD_BUILD_TOOLS "Build tools for dnmd" ON)

if (DNMD_BUILD_INTERFACES OR DNMD_BUILD_TESTS OR DNMD_BUILD_TOOLS)
    enable_language(CXX)
endif()

add_subdirectory(src/)


if (DNMD_BUILD_TESTS)
    enable_testing()

    add_subdirectory(test/)
endif()