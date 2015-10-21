cmake_minimum_required (VERSION 3.1)
project (corehost)
set (CMAKE_CXX_STANDARD 11)
include_directories(inc)

# CMake does not recommend using globbing since it messes with the freshness checks
set(SOURCES
    src/args.cpp
    src/main.cpp
    src/tpafile.cpp
    src/trace.cpp
    src/utils.cpp
    src/coreclr.cpp

    inc/args.h
    inc/pal.h
    inc/tpafile.h
    inc/trace.h
    inc/coreclr.h
    inc/utils.h)

if(WIN32)
    list(APPEND SOURCES src/pal.windows.cpp)
else()
    list(APPEND SOURCES src/pal.unix.cpp)
endif()

add_executable(corehost ${SOURCES})

if(${CMAKE_SYSTEM_NAME} MATCHES "Linux")
    add_definitions(-D__LINUX__)
    target_link_libraries (corehost "dl")
endif()
