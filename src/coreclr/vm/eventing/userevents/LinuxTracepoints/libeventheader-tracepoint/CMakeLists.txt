cmake_minimum_required(VERSION 3.10)
project(eventheader-tracepoint
    VERSION 1.3.3
    DESCRIPTION "EventHeader-encoded Linux tracepoints for C/C++"
    HOMEPAGE_URL "https://github.com/microsoft/LinuxTracepoints"
    LANGUAGES C CXX)
include(GNUInstallDirs)
include(CMakePackageConfigHelpers)
set(BUILD_SAMPLES ON CACHE BOOL "Build sample code")
set(BUILD_TOOLS ON CACHE BOOL "Build tool code")

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
        find_package(tracepoint-headers 1.3 REQUIRED)
    endif()

    if(NOT TARGET tracepoint)
        find_package(tracepoint 1.3 REQUIRED)
    endif()

    add_subdirectory(src)

endif()
