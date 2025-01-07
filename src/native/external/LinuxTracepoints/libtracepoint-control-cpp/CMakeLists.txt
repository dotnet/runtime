cmake_minimum_required(VERSION 3.10)
include(../version.cmake)
project(tracepoint-control-cpp
    VERSION ${LINUXTRACEPOINTS_VERSION}
    DESCRIPTION "Linux tracepoint collection for C++"
    HOMEPAGE_URL "https://github.com/microsoft/LinuxTracepoints"
    LANGUAGES CXX)
include(GNUInstallDirs)
include(CMakePackageConfigHelpers)
set(BUILD_SAMPLES ON CACHE BOOL "Build sample code")
set(BUILD_TOOLS ON CACHE BOOL "Build tool code")

if(NOT WIN32)

    if(NOT TARGET tracepoint-decode)
        find_package(tracepoint-decode ${TRACEPOINT_DECODE_MINVER} REQUIRED)
    endif()

    add_compile_options(
        -Wall
        -Wextra
        -Wformat
        -Wformat-security
        -Werror=format-security
        -Wstack-protector
        -Werror=stack-protector)
    if(NOT CMAKE_BUILD_TYPE STREQUAL "Debug")
        add_compile_options(-D_FORTIFY_SOURCE=2)
    endif()

    add_subdirectory(src)

    if(BUILD_SAMPLES)
        add_subdirectory(samples)
    endif()

    if(BUILD_TOOLS)
        add_subdirectory(tools)
    endif()

endif()
