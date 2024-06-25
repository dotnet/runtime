cmake_minimum_required(VERSION 3.10)
include(../version.cmake)
project(tracepoint-decode-cpp
    VERSION ${LINUXTRACEPOINTS_VERSION}
    DESCRIPTION "Tracepoint decoding for C/C++"
    HOMEPAGE_URL "https://github.com/microsoft/LinuxTracepoints"
    LANGUAGES CXX)
include(GNUInstallDirs)
include(CMakePackageConfigHelpers)

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

add_subdirectory(src)
add_subdirectory(samples)
