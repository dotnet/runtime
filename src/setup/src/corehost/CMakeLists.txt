cmake_minimum_required (VERSION 2.6)

set(VERSION_FILE_PATH "${CMAKE_BINARY_DIR}/../../../version.cpp")

include(../settings.cmake)
add_subdirectory(cli)
