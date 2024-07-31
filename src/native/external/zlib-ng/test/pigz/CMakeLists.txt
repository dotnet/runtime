# CMakeLists.txt -- Build madler/pigz against zlib variant

# Copyright (C) 2021 Nathan Moinvaziri
# Licensed under the Zlib license, see LICENSE.md for details

# By default pigz will be linked against the system zlib and
# pthread libraries if installed.

# For compilation on Windows download and use shim:
#  https://github.com/zlib-ng/pigzbench/tree/master/pigz/win

# Optional Variables
#   WITH_CODE_COVERAGE  - Enable code coverage reporting
#   WITH_THREADS        - Enable threading support
#   PIGZ_ENABLE_TESTS   - Enable adding unit tests
#   PIGZ_VERSION        - Set the version of pigz to build
#   ZLIB_ROOT           - Path to the zlib source directory
#   PTHREADS4W_ROOT     - Path to pthreads4w source directory on Windows.
#                         If not specified then threading will be disabled.

cmake_minimum_required(VERSION 3.11)

include(CheckCCompilerFlag)
include(FeatureSummary)
include(FetchContent)

include(../../cmake/detect-coverage.cmake)

option(WITH_CODE_COVERAGE "Enable code coverage reporting" OFF)
option(WITH_THREADS "Enable threading support" ON)
option(PIGZ_ENABLE_TESTS "Build unit tests" ON)
option(PIGZ_VERSION "Set the version of pigz to build" "")

project(pigz LANGUAGES C)

# Set code coverage compiler flags
if(WITH_CODE_COVERAGE)
    add_code_coverage()
endif()

# Compiler definitions
if(CMAKE_C_COMPILER_ID STREQUAL "Clang")
    add_definitions(-fno-caret-diagnostics)
elseif(CMAKE_C_COMPILER_ID STREQUAL "GNU")
    if(NOT CMAKE_C_COMPILER_VERSION VERSION_LESS 4.5.0)
        add_definitions(-Wno-unused-result)
    endif()
    if(NOT CMAKE_C_COMPILER_VERSION VERSION_LESS 4.8.0)
        add_definitions(-fno-diagnostics-show-caret)
    endif()
elseif(WIN32)
    add_definitions(-D_TIMESPEC_DEFINED)
    if(MSVC)
        add_definitions(-D_CRT_SECURE_NO_DEPRECATE)
    endif()
endif()

# Fetch pigz source code from official repository
if(PIGZ_VERSION STREQUAL "")
    set(PIGZ_TAG master)
else()
    set(PIGZ_TAG ${PIGZ_VERSION})
endif()
FetchContent_Declare(pigz
    GIT_REPOSITORY https://github.com/madler/pigz.git
    GIT_TAG ${PIGZ_TAG})

FetchContent_GetProperties(pigz)
if(NOT pigz_POPULATED)
    FetchContent_Populate(pigz)
endif()

set(PIGZ_SRCS
    ${pigz_SOURCE_DIR}/pigz.c
    ${pigz_SOURCE_DIR}/try.c)

set(PIGZ_HDRS
    ${pigz_SOURCE_DIR}/try.h)

add_executable(${PROJECT_NAME} ${PIGZ_SRCS} ${PIGZ_HDRS})
add_definitions(-DNOZOPFLI)
if(WIN32)
    target_include_directories(${PROJECT_NAME} PRIVATE win)
endif()

# Find and link against pthreads or pthreads4w
if(WITH_THREADS)
    if(WIN32)
        if(DEFINED PTHREADS4W_ROOT)
            set(CLEANUP_STYLE VC)
            set(PTHREADS4W_VERSION 3)

            add_subdirectory(${PTHREADS4W_ROOT} ${PTHREADS4W_ROOT} EXCLUDE_FROM_ALL)
            target_link_libraries(${PROJECT_NAME} pthreadVC3)
            target_include_directories(${PROJECT_NAME} PRIVATE ${PTHREADS4W_ROOT})
        else()
            message(WARNING "Missing pthreads4w root directory")
            set(WITH_THREADS OFF)
        endif()
    else()
        find_package(Threads REQUIRED)
        target_link_libraries(${PROJECT_NAME} Threads::Threads)
        if(NOT APPLE)
            target_link_libraries(${PROJECT_NAME} m)
        endif()
    endif()
endif()

# Disable threading support
if(NOT WITH_THREADS)
    add_definitions(-DNOTHREAD)
else()
    set_property(TARGET ${PROJECT_NAME} APPEND PROPERTY SOURCES
        ${pigz_SOURCE_DIR}/yarn.c
        ${pigz_SOURCE_DIR}/yarn.h)
endif()

# Find and link against zlib
if(NOT DEFINED ZLIB_ROOT)
    find_package(Zlib REQUIRED)
endif()

set(ZLIB_COMPAT ON)
set(ZLIB_ENABLE_TESTS OFF)

add_subdirectory(${ZLIB_ROOT} ${CMAKE_CURRENT_BINARY_DIR}/zlib EXCLUDE_FROM_ALL)

if(NOT DEFINED BUILD_SHARED_LIBS OR NOT BUILD_SHARED_LIBS)
    set(ZLIB_TARGET zlibstatic)
else()
    set(ZLIB_TARGET zlib)
endif()

target_include_directories(${PROJECT_NAME} PRIVATE ${ZLIB_ROOT} ${CMAKE_CURRENT_BINARY_DIR}/zlib)
target_link_libraries(${PROJECT_NAME} ${ZLIB_TARGET})

if(NOT SKIP_INSTALL_BINARIES AND NOT SKIP_INSTALL_ALL)
    install(TARGETS ${PROJECT_NAME} DESTINATION "bin")
endif()

# Add unit tests
if(PIGZ_ENABLE_TESTS)
    enable_testing()

    set(PIGZ_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:pigz>)

    macro(test_pigz name path)
        # Construct compression arguments for pigz
        set(compress_args -k -c)
        foreach(extra_arg IN ITEMS "${ARGN}")
            list(APPEND compress_args ${extra_arg})
        endforeach()

        # Create unique friendly string for test
        string(REPLACE ";" "" arg_list "${ARGN}")
        string(REPLACE " " "" arg_list "${arg_list}")
        string(REPLACE "-" "" arg_list "${arg_list}")

        set(test_id pigz-${name}-${arg_list})

        if(NOT TEST ${test_id})
            add_test(NAME ${test_id}
                COMMAND ${CMAKE_COMMAND}
                "-DTARGET=${PIGZ_COMMAND}"
                "-DCOMPRESS_ARGS=${compress_args}"
                "-DDECOMPRESS_ARGS=-d;-c"
                -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/${path}
                -DTEST_NAME=${test_id}
                -P ${CMAKE_CURRENT_SOURCE_DIR}/../cmake/compress-and-verify.cmake)
        endif()
    endmacro()

    set(TEST_CONFIGS
        -U       # RLE compression
        #-H      # Z_HUFFMAN_ONLY (broken in 2.6)
        -0      # No compression
        -1      # Deflate quick
        -4      # Deflate medium (lazy matches)
        -6      # Deflate medium
        -9      # Deflate slow
    )

    file(GLOB_RECURSE TEST_FILE_PATHS
        LIST_DIRECTORIES false
        RELATIVE ${CMAKE_CURRENT_SOURCE_DIR}
        ${CMAKE_CURRENT_SOURCE_DIR}/../data/*)

    foreach(TEST_FILE_PATH ${TEST_FILE_PATHS})
        if("${TEST_FILE_PATH}" MATCHES ".gz$" OR "${TEST_FILE_PATH}" MATCHES ".out$" OR
           "${TEST_FILE_PATH}" MATCHES "/.git/" OR "${TEST_FILE_PATH}" MATCHES ".md$")
            continue()
        endif()
        foreach(TEST_CONFIG ${TEST_CONFIGS})
            get_filename_component(TEST_NAME ${TEST_FILE_PATH} NAME)
            if (TEST_NAME STREQUAL "")
                continue()
            endif()
            test_pigz(${TEST_NAME} ${TEST_FILE_PATH} ${TEST_CONFIG})
        endforeach()
    endforeach()

    set(GH979_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:pigz>
        -d -k -f ${CMAKE_CURRENT_SOURCE_DIR}/../GH-979/pigz-2.6.tar.gz)
    add_test(NAME GH-979 COMMAND ${GH979_COMMAND})
endif()

add_feature_info(WITH_CODE_COVERAGE WITH_CODE_COVERAGE "Enable code coverage reporting")
add_feature_info(WITH_THREADS WITH_THREADS "Enable threading support")
add_feature_info(PIGZ_ENABLE_TESTS PIGZ_ENABLE_TESTS "Build unit tests")

FEATURE_SUMMARY(WHAT ALL INCLUDE_QUIET_PACKAGES)
