cmake_minimum_required(VERSION 3.10)
project(dnmd
    LANGUAGES C CXX)

set(CMAKE_INSTALL_PREFIX ${CMAKE_BINARY_DIR})

add_subdirectory(src/)
add_subdirectory(test/)
add_subdirectory(external/dncp)
