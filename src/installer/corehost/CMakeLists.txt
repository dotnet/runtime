cmake_minimum_required(VERSION 3.14.2)

project(corehost)

include(${CLR_ENG_NATIVE_DIR}/configuretools.cmake)
include(../settings.cmake)
include(../functions.cmake)
add_subdirectory(cli)
