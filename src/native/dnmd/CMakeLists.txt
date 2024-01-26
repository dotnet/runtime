cmake_minimum_required(VERSION 3.10)
project(dnmd
    LANGUAGES C CXX)

option(INCLUDE_VENDORED_LIBS "Include vendored libraries (submodules) instead of discovering dependencies through packages." ON)

set(CMAKE_INSTALL_PREFIX ${CMAKE_BINARY_DIR})

if (INCLUDE_VENDORED_LIBS)
    add_subdirectory(external/dncp)
else()
    find_package(dncp REQUIRED)
endif()

include_directories(src/inc)
include_directories(src/inc/external) # Hiding the "external" subdirectory due to uses of <...> in cor.h.

add_subdirectory(src/)

enable_testing()

add_subdirectory(test/)

