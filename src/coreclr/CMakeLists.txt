cmake_minimum_required(VERSION 3.6.2)

cmake_policy(SET CMP0042 NEW)

# Set the project name
project(CoreCLR)

include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

if (CLR_CMAKE_HOST_WIN32)
  message(STATUS "VS_PLATFORM_TOOLSET is ${CMAKE_VS_PLATFORM_TOOLSET}")
  message(STATUS "VS_PLATFORM_NAME is ${CMAKE_VS_PLATFORM_NAME}")
endif (CLR_CMAKE_HOST_WIN32)

if(MSVC)
  add_compile_options(/EHa) # enable C++ EH (w/ SEH exceptions)
  set(CMAKE_CXX_STANDARD_LIBRARIES "") # do not link against standard win32 libs i.e. kernel32, uuid, user32, etc.
endif (MSVC)

# Set commonly used directory names
set(CLR_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(VM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/src/vm)
set(GENERATED_INCLUDE_DIR ${CMAKE_CURRENT_BINARY_DIR}/src/inc)
set(GENERATED_EVENTING_DIR ${CMAKE_CURRENT_BINARY_DIR}/Eventing)
set(VERSION_FILE_PATH "${CMAKE_BINARY_DIR}/version.c")
set(PAL_REDEFINES_FILE ${CMAKE_CURRENT_SOURCE_DIR}/src/dlls/mscordac/palredefines.S)

# Avoid logging when skipping up-to-date copies
set(CMAKE_INSTALL_MESSAGE LAZY)

set(CORECLR_SET_RPATH ON)
if(CORECLR_SET_RPATH)
    # Enable @rpath support for shared libraries.
    set(MACOSX_RPATH ON)
endif(CORECLR_SET_RPATH)

OPTION(CLR_CMAKE_ENABLE_CODE_COVERAGE "Enable code coverage" OFF)

#----------------------------------------------------
# Cross target Component build specific configuration
#----------------------------------------------------
if(CLR_CROSS_COMPONENTS_BUILD)
  include(crosscomponents.cmake)
endif(CLR_CROSS_COMPONENTS_BUILD)

#-------------------
# Enable PGO support
#-------------------
include(pgosupport.cmake)

#-------------------------------
# Include libraries native shims
#-------------------------------
if(NOT CLR_CROSS_COMPONENTS_BUILD)
  add_subdirectory(src/libraries-native)
endif(NOT CLR_CROSS_COMPONENTS_BUILD)

#-----------------------------------------
# Add Projects
#     - project which require platform header not clr's
#     - do not depend on clr's compile definitions
#-----------------------------------------
if(CLR_CMAKE_HOST_UNIX)
    if(CLR_CMAKE_TARGET_ANDROID)
        find_library(LZMA NAMES lzma)
        if(LZMA STREQUAL LZMA-NOTFOUND)
           message(FATAL_ERROR "Cannot find liblzma.")
        endif(LZMA STREQUAL LZMA-NOTFOUND)

        find_library(ANDROID_GLOB NAMES android-glob)
        if(ANDROID_GLOB STREQUAL ANDROID_GLOB-NOTFOUND)
          message(FATAL_ERROR "Cannot find android-glob.")
        endif()
    endif()

    add_subdirectory(src/pal)
    add_subdirectory(src/hosts)
else(CLR_CMAKE_HOST_UNIX)
    if(CLR_CMAKE_TARGET_UNIX)
        add_subdirectory(src/pal/src/libunwind)
    endif(CLR_CMAKE_TARGET_UNIX)
endif(CLR_CMAKE_HOST_UNIX)

# Add this subdir. We install the headers for the jit.
add_subdirectory(src/pal/prebuilt/inc)

add_subdirectory(src/debug/debug-pal)

if(CLR_CMAKE_TARGET_WIN32)
  add_subdirectory(src/gc/sample)
endif()

add_subdirectory(src/tools/aot/jitinterface)

# Above projects do not build with these compile options
# All of the compiler options are specified in file compileoptions.cmake
# Do not add any new options here. They should be added in compileoptions.cmake
if(CLR_CMAKE_HOST_WIN32)
  add_compile_options(/Zl) # omit default library name in .OBJ
endif(CLR_CMAKE_HOST_WIN32)

#--------------------------------
# Definition directives
#  - all clr specific compile definitions should be included in this file
#  - all clr specific feature variable should also be added in this file
#----------------------------------
include(clrdefinitions.cmake)

#-------------------------------------
# Include directory directives
#-------------------------------------
# Include the basic prebuilt headers - required for getting fileversion resource details.
include_directories("src/pal/prebuilt/inc")
include_directories("../../artifacts/obj/coreclr")

if(FEATURE_STANDALONE_GC)
  add_definitions(-DFEATURE_STANDALONE_GC)
  add_subdirectory(src/gc)
endif(FEATURE_STANDALONE_GC)

if (CLR_CMAKE_HOST_UNIX)
  include_directories("src/pal/inc")
  include_directories("src/pal/inc/rt")
  include_directories("src/pal/src/safecrt")
endif (CLR_CMAKE_HOST_UNIX)

#------------------------------
# Add Product Directory
#------------------------------
add_subdirectory(src)
