cmake_minimum_required(VERSION 2.8.12)

if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

project(CoreFX)
  
set(CMAKE_INSTALL_PREFIX $ENV{__CMakeBinDir})
set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(CMAKE_CXX_FLAGS "-std=c++11")
set(CMAKE_SHARED_LIBRARY_PREFIX "")

if (CMAKE_CXX_COMPILER_VERSION VERSION_LESS 3.5)
    add_compile_options(-Wno-unreachable-code)
endif ()

if (CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64)
    add_definitions(-DBIT64=1)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l)
    add_definitions(-DBIT32=1)
    # Because we don't use CMAKE_C_COMPILER/CMAKE_CXX_COMPILER to use clang
    # we have to set the triple by adding a compiler argument
    add_compile_options(-target armv7-linux-gnueabihf)
    add_compile_options(-mthumb)
    add_compile_options(-mfpu=vfpv3)
endif ()

string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)
if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
    add_definitions(-DDEBUG)
elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
    add_definitions(-DNDEBUG)
else ()
    message(FATAL_ERROR "Unknown build type. Set CMAKE_BUILD_TYPE to DEBUG or RELEASE.")
endif ()

add_subdirectory(clrcompression)