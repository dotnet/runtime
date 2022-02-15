project(libunwind)

cmake_minimum_required(VERSION 3.16.1)

set(PKG_MAJOR "1")
set(PKG_MINOR "6")
set(PKG_EXTRA "-rc1")
set(PACKAGE_STRING "libunwind")
set(PACKAGE_BUGREPORT "")


if ('$ENV{TARGET}' STREQUAL 'x86_64-linux-gnu')
  set(TARGET_AMD64 1)
  set(arch x86_64)
  add_definitions(-D__x86_64__)
  add_definitions(-D__amd64__)
  add_definitions(-D__linux__)
elseif ('$ENV{TARGET}' STREQUAL 'aarch64-linux-gnu')
  set(TARGET_AARCH64 1)
  set(arch aarch64)
  add_definitions(-D__aarch64__)
  add_definitions(-D__linux__)
elseif ('$ENV{TARGET}' STREQUAL 'arm-linux-gnueabihf')
  set(TARGET_ARM 1)
  set(arch arm)
  add_definitions(-D__arm__)
  add_definitions(-D__linux__)
elseif ('$ENV{TARGET}' STREQUAL 's390x-linux-gnu')
  set(TARGET_S390X 1)
  set(arch s390x)
  add_definitions(-D__s390x__)
  add_definitions(-D__linux__)
elseif ('$ENV{TARGET}' STREQUAL 'loongarch64-linux-gnu')
  set(TARGET_LOONGARCH64 1)
  set(arch loongarch64)
  add_definitions(-D__loongarch64)
  add_definitions(-D__linux__)
else ()
  message(FATAL_ERROR "Unrecognize value in environment variable TARGET")
endif ()

include(CheckCSourceCompiles)
include(CheckIncludeFiles)

if ("${CMAKE_GENERATOR}" MATCHES "^Visual Studio.*$")
  message(VERBOSE "Using generator ${CMAKE_GENERATOR}")
  # Assume we are using default MSVC compiler
  add_compile_options(/std:c++latest)
  add_compile_options(/TC) # compile all files as C
  add_compile_options(/permissive-)

  # files for cross os compilation
  include_directories(include/win)

  # Warnings in release builds
  add_compile_options(-wd4068) # ignore unknown pragma warnings (gcc pragmas)
  add_compile_options(-wd4146) # minus operator applied to unsigned
  add_compile_options(-wd4244) # possible loss of data
  add_compile_options(-wd4267) # possible loss of data
  add_compile_options(-wd4334) # 32-bit shift implicitly converted to 64 bits

  # Disable warning due to incorrect format specifier in debugging printf via the Debug macro
  add_compile_options(-wd4311) # pointer truncation from 'unw_word_t *' to 'long'
  add_compile_options(-wd4475) # 'fprintf' : length modifier 'L' cannot be used
  add_compile_options(-wd4477) # fprintf argument type

  # Windows builds will only support remote unwind
  add_definitions(-DUNW_REMOTE_ONLY)

  # Disable security warnings
  add_definitions(-D_CRT_SECURE_NO_WARNINGS)

  # Our posix abstraction layer will provide these headers
  set(HAVE_ELF_H 1)
  set(HAVE_ENDIAN_H 1)

  # MSVC compiler is currently missing C11 stdalign.h header
  # Fake it until support is added
  check_include_files(stdalign.h HAVE_STDALIGN_H)
  if (NOT HAVE_STDALIGN_H)
    configure_file(include/win/fakestdalign.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/stdalign.h)
  endif (NOT HAVE_STDALIGN_H)

  # MSVC compiler is currently missing C11 stdatomic.h header
  # Fake it until support is added
  check_include_files(stdatomic.h HAVE_STDATOMIC_H)
  if (NOT HAVE_STDATOMIC_H)
    configure_file(include/win/fakestdatomic.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/stdatomic.h)
  endif (NOT HAVE_STDATOMIC_H)
  
  # MSVC compiler is currently missing C11 _Thread_local
  check_c_source_compiles("void main() { _Thread_local int a; }"  HAVE_THREAD_LOCAL)
  if (NOT HAVE_THREAD_LOCAL)
    add_definitions(-D_Thread_local=)
  endif (NOT HAVE_THREAD_LOCAL)
else ()
  message(FATAL_ERROR "This CMake file is currently only designed for building on Visual Studio")
endif ()

add_definitions(-DHAVE_CONFIG_H)

configure_file(include/config.h.cmake.in ${CMAKE_CURRENT_BINARY_DIR}/include/config.h)
configure_file(include/libunwind-common.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind-common.h)
configure_file(include/libunwind.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/libunwind.h)
configure_file(include/tdep/libunwind_i.h.in ${CMAKE_CURRENT_BINARY_DIR}/include/tdep/libunwind_i.h)

add_subdirectory(src)

