cmake_minimum_required(VERSION 3.10)
project(tracepoint
    VERSION 1.3.3
    DESCRIPTION "Linux tracepoints interface for C/C++"
    HOMEPAGE_URL "https://github.com/microsoft/LinuxTracepoints"
    LANGUAGES C CXX)
include(GNUInstallDirs)
include(CMakePackageConfigHelpers)
set(BUILD_SAMPLES ON CACHE BOOL "Build sample code")
set(BUILD_TESTS ON CACHE BOOL "Build test code")
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
    add_subdirectory(src)
endif()
