cmake_minimum_required(VERSION 2.8.12)
project(CoreFX)

set(CMAKE_INSTALL_PREFIX $ENV{__CMakeBinDir})
set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(CMAKE_C_FLAGS "-std=c11")
set(CMAKE_CXX_FLAGS "-std=c++11")
set(CMAKE_SHARED_LIBRARY_PREFIX "")
add_compile_options(-Wall -Werror -fPIC)

if (CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64)
    add_definitions(-DBIT64=1)
endif ()

string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)
if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
    add_compile_options(-g -O0)
    add_definitions(-DDEBUG)
elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
    add_compile_options (-O3)
    add_definitions(-DNDEBUG)
else ()
    message(FATAL_ERROR "Unknown build type. Set CMAKE_BUILD_TYPE to DEBUG or RELEASE.")
endif ()

add_subdirectory(System.IO.Native)
add_subdirectory(System.Security.Cryptography.Native)

