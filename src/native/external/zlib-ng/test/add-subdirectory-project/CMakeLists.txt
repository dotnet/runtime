cmake_minimum_required(VERSION 3.5.1)

project(zlib-ng-add-subdirecory-test C)

include(CTest)

set(BUILD_SHARED_LIBS OFF)
set(ZLIB_ENABLE_TESTS ON CACHE BOOL "Build test binaries" FORCE)

add_subdirectory(../.. zlib-ng)

add_executable(app main.c)
target_link_libraries(app zlibstatic)
