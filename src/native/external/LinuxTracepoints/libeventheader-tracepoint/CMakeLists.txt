cmake_minimum_required(VERSION 3.10)
include(../version.cmake)
project(eventheader-tracepoint
    VERSION ${LINUXTRACEPOINTS_VERSION}
    DESCRIPTION "EventHeader-encoded Linux tracepoints for C/C++"
    HOMEPAGE_URL "https://github.com/microsoft/LinuxTracepoints"
    LANGUAGES C CXX)
include(GNUInstallDirs)
include(CMakePackageConfigHelpers)
set(BUILD_SAMPLES ON CACHE BOOL "Build sample code")

if(WIN32)
    add_compile_options(/W4 /WX /permissive-)
else()
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
endif()

add_subdirectory(include)

if(NOT WIN32)

    if(NOT TARGET tracepoint-headers)
        find_package(tracepoint-headers ${TRACEPOINT_HEADERS_MINVER} REQUIRED)
    endif()

    if(NOT TARGET tracepoint)
        find_package(tracepoint ${TRACEPOINT_MINVER} REQUIRED)
    endif()

    add_subdirectory(src)

    if(BUILD_SAMPLES)
        add_subdirectory(samples)
    endif()

endif()
