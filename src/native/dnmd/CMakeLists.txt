project(dnmd
    LANGUAGES C CXX)
cmake_minimum_required(VERSION 3.10)

set(CMAKE_INSTALL_PREFIX ${CMAKE_BINARY_DIR})

add_subdirectory(src/)
add_subdirectory(test/)
add_subdirectory(external/dncp)
