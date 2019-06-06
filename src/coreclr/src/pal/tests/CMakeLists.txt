cmake_minimum_required(VERSION 2.8.12.2)

if(CLR_CMAKE_PLATFORM_ARCH_I386)
    set(PAL_CMAKE_PLATFORM_ARCH_I386 1)
elseif(CLR_CMAKE_PLATFORM_ARCH_AMD64)
    set(PAL_CMAKE_PLATFORM_ARCH_AMD64 1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
    set(PAL_CMAKE_PLATFORM_ARCH_ARM 1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    set(PAL_CMAKE_PLATFORM_ARCH_ARM64 1)
endif()

# Compile options
add_definitions(-DPLATFORM_UNIX)
add_definitions(-DLP64COMPATIBLE)
add_definitions(-DFEATURE_PAL)
add_definitions(-DCORECLR)
add_definitions(-DPIC)
if(PAL_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-DBIT32)
elseif(PAL_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-DBIT64)
  add_definitions(-D_WIN64)
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-DBIT32)
elseif(PAL_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-DBIT64)
  add_definitions(-D_WIN64)
else()
  message(FATAL_ERROR "Only ARM, ARM64, I386, and AMD64 is supported")
endif()

# C++ emits errors and warnings for c-string literal fed into char* parameter
# this is just to take care of the warnings
if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wno-writable-strings)
elseif (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  add_compile_options(-Wno-write-strings)
endif()

add_compile_options(-Wno-empty-body)

add_subdirectory(palsuite)


