cmake_minimum_required(VERSION 3.10)
include(version.cmake)
project(LinuxTracepoints
    VERSION ${LINUXTRACEPOINTS_VERSION})

set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)

add_subdirectory(libtracepoint)
add_subdirectory(libtracepoint-decode-cpp)
add_subdirectory(libeventheader-tracepoint)
add_subdirectory(libeventheader-decode-cpp)

if(NOT WIN32)
    add_subdirectory(libtracepoint-control-cpp)
endif()
