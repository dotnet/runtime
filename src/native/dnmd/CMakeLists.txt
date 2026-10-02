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
option(DNMD_ENABLE_FUZZING "Build with fuzzing support." OFF)

set(DNMD_LOADED_MODULES_CACHE_DEFAULT OFF)
if ("${CMAKE_CURRENT_SOURCE_DIR}" STREQUAL "${CMAKE_SOURCE_DIR}" AND
    (DNMD_BUILD_TESTS OR DNMD_ENABLE_FUZZING))
    set(DNMD_LOADED_MODULES_CACHE_DEFAULT ON)
endif()
option(DNMD_ENABLE_LOADED_MODULES_CACHE "Track live metadata scopes for cross-scope TypeRef resolution" ${DNMD_LOADED_MODULES_CACHE_DEFAULT})

set(DNMD_INTERNAL_INTERFACES_DEFAULT OFF)
if ("${CMAKE_CURRENT_SOURCE_DIR}" STREQUAL "${CMAKE_SOURCE_DIR}" AND
    (DNMD_BUILD_TESTS OR DNMD_ENABLE_FUZZING))
    set(DNMD_INTERNAL_INTERFACES_DEFAULT ON)
endif()
option(DNMD_ENABLE_INTERNAL_INTERFACES "Build CoreCLR-specific metadata interfaces and helpers" ${DNMD_INTERNAL_INTERFACES_DEFAULT})

if (DNMD_ENABLE_LOADED_MODULES_CACHE AND NOT DNMD_ENABLE_INTERNAL_INTERFACES)
    message(FATAL_ERROR "DNMD_ENABLE_LOADED_MODULES_CACHE requires DNMD_ENABLE_INTERNAL_INTERFACES")
endif()

if (DNMD_BUILD_INTERFACES OR DNMD_BUILD_TESTS OR DNMD_BUILD_TOOLS OR DNMD_ENABLE_FUZZING)
    enable_language(CXX)
endif()

if (DNMD_BUILD_TESTS OR DNMD_ENABLE_FUZZING)
    include(FetchContent)

    if (POLICY CMP0135)
        cmake_policy(SET CMP0135 NEW) # Set timestamps in downloaded archives to the time of download.
    endif()

    if (DNMD_ENABLE_FUZZING)
        message(STATUS "Enabling fuzzing support.")
        FetchContent_Declare(
            fuzztest
            GIT_REPOSITORY
            https://github.com/google/fuzztest.git
            GIT_TAG
            2025-08-05
        )

        set(ANTLR_BUILD_CPP_TESTS OFF CACHE BOOL "" FORCE)
        set(FUZZTEST_FUZZING_MODE ON)

        FetchContent_MakeAvailable(fuzztest)

        fuzztest_setup_fuzzing_flags()
    endif()
endif()

add_subdirectory(src/)

if (DNMD_BUILD_TESTS OR DNMD_ENABLE_FUZZING)
    enable_testing()

    add_subdirectory(test/)
endif()
