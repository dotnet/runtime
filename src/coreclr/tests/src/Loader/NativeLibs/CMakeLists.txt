cmake_minimum_required(VERSION 2.6)
project(FromNativePaths_lib)

set(CMAKE_SHARED_LIBRARY_PREFIX "")

set(SOURCES FromNativePaths_lib.cpp FromNativePaths_lib.def)
add_library(FromNativePaths_lib SHARED ${SOURCES})

install(TARGETS FromNativePaths_lib DESTINATION Loader/NativeLibs)
