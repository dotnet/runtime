cmake_minimum_required(VERSION 3.6.2)

if (CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    # CMake 3.14.5 contains bug fixes for iOS
    cmake_minimum_required(VERSION 3.14.5)
elseif (CLR_CMAKE_TARGET_MACCATALYST)
    # CMake 3.18.1 properly generates MacCatalyst C compiler
    cmake_minimum_required(VERSION 3.18.1)
endif ()

if (WIN32)
    cmake_policy(SET CMP0091 NEW)
else ()
    cmake_policy(SET CMP0042 NEW)
endif ()

project(watchdog)

include(../../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(CMAKE_CXX_STANDARD 11)

add_executable(watchdog watchdog.cpp)

