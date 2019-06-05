# Verify minimum required version
cmake_minimum_required(VERSION 3.5.1)

cmake_policy(SET CMP0042 NEW)

# Set the project name
project(CoreCLR)

# Include cmake functions
include(functions.cmake)

# Verify that LTCG/LTO is available
include(configure.cmake)

if (WIN32)
  message(STATUS "VS_PLATFORM_TOOLSET is ${CMAKE_VS_PLATFORM_TOOLSET}")
  message(STATUS "VS_PLATFORM_NAME is ${CMAKE_VS_PLATFORM_NAME}")
endif (WIN32)

# Set commonly used directory names
set(CLR_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(VM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/src/vm)
set(GENERATED_INCLUDE_DIR ${CMAKE_CURRENT_BINARY_DIR}/src/inc)
set(GENERATED_EVENTING_DIR ${CMAKE_CURRENT_BINARY_DIR}/Eventing)
set(VERSION_FILE_PATH "${CMAKE_BINARY_DIR}/version.c")
set(PAL_REDEFINES_FILE ${CMAKE_CURRENT_SOURCE_DIR}/src/dlls/mscordac/palredefines.S)

set(CORECLR_SET_RPATH ON)
if(CORECLR_SET_RPATH)
    # Enable @rpath support for shared libraries.
    set(MACOSX_RPATH ON)
endif(CORECLR_SET_RPATH)

OPTION(CLR_CMAKE_ENABLE_CODE_COVERAGE "Enable code coverage" OFF)
OPTION(CLR_CMAKE_WARNINGS_ARE_ERRORS "Warnings are errors" ON)

# Ensure other tools are present
if (WIN32)
    if(CLR_CMAKE_HOST_ARCH STREQUAL arm)

      # Confirm that Windows SDK is present
      if(NOT DEFINED CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION OR CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION STREQUAL "" )
	      message(FATAL_ERROR "Windows SDK is required for the Arm32 build.")
      else()
	      message("Using Windows SDK version ${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}")
      endif()

      # Explicitly specify the assembler to be used for Arm32 compile
      file(TO_CMAKE_PATH "$ENV{VCToolsInstallDir}\\bin\\HostX86\\arm\\armasm.exe" CMAKE_ASM_COMPILER)

      set(CMAKE_ASM_MASM_COMPILER ${CMAKE_ASM_COMPILER})
      message("CMAKE_ASM_MASM_COMPILER explicitly set to: ${CMAKE_ASM_MASM_COMPILER}")

      # Enable generic assembly compilation to avoid CMake generate VS proj files that explicitly
      # use ml[64].exe as the assembler.
      enable_language(ASM)
    elseif(CLR_CMAKE_HOST_ARCH STREQUAL arm64)

      # Confirm that Windows SDK is present
      if(NOT DEFINED CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION OR CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION STREQUAL "" )
	      message(FATAL_ERROR "Windows SDK is required for the ARM64 build.")
      else()
	      message("Using Windows SDK version ${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}")
      endif()

      # Explicitly specify the assembler to be used for Arm64 compile
      file(TO_CMAKE_PATH "$ENV{VCToolsInstallDir}\\bin\\HostX86\\arm64\\armasm64.exe" CMAKE_ASM_COMPILER)

      set(CMAKE_ASM_MASM_COMPILER ${CMAKE_ASM_COMPILER})
      message("CMAKE_ASM_MASM_COMPILER explicitly set to: ${CMAKE_ASM_MASM_COMPILER}")

      # Enable generic assembly compilation to avoid CMake generate VS proj files that explicitly
      # use ml[64].exe as the assembler.
      enable_language(ASM)
    else()
      enable_language(ASM_MASM)
    endif()

    # Ensure that MC is present
    find_program(MC mc)
    if (MC STREQUAL "MC-NOTFOUND")
        message(FATAL_ERROR "MC not found")
    endif()

else (WIN32)
    enable_language(ASM)

    # Ensure that awk is present
    find_program(AWK awk)
    if (AWK STREQUAL "AWK-NOTFOUND")
        message(FATAL_ERROR "AWK not found")
    endif()

    # Try to locate the paxctl tool. Failure to find it is not fatal,
    # but the generated executables won't work on a system where PAX is set
    # to prevent applications to create executable memory mappings.
    find_program(PAXCTL paxctl)

    if (CMAKE_SYSTEM_NAME STREQUAL Darwin)

      # Ensure that dsymutil and strip are present
      find_program(DSYMUTIL dsymutil)
      if (DSYMUTIL STREQUAL "DSYMUTIL-NOTFOUND")
          message(FATAL_ERROR "dsymutil not found")
      endif()

      find_program(STRIP strip)
      if (STRIP STREQUAL "STRIP-NOTFOUND")
          message(FATAL_ERROR "strip not found")
      endif()

    else (CMAKE_SYSTEM_NAME STREQUAL Darwin)

      # Ensure that objcopy is present
      if (CLR_UNIX_CROSS_BUILD AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
        if (CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l OR CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL arm)
          find_program(OBJCOPY ${TOOLCHAIN}-objcopy)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
          find_program(OBJCOPY objcopy)
        else()
          clr_unknown_arch()
        endif()
      else()
        find_program(OBJCOPY objcopy)
      endif()

      if (OBJCOPY STREQUAL "OBJCOPY-NOTFOUND")
          message(FATAL_ERROR "objcopy not found")
      endif()

    endif (CMAKE_SYSTEM_NAME STREQUAL Darwin)
endif(WIN32)

#----------------------------------------------------
# Configure compiler settings for environment
#----------------------------------------------------
include(configurecompiler.cmake)

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

#-----------------------------------------
# Add Projects
#     - project which require platform header not clr's
#     - do not depend on clr's compile definitions
#-----------------------------------------
if(CLR_CMAKE_PLATFORM_UNIX AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
    add_subdirectory(src/corefx)
endif()

if(CLR_CMAKE_PLATFORM_UNIX)
    add_subdirectory(src/ToolBox/SOS/lldbplugin)
    add_subdirectory(src/pal)
    add_subdirectory(src/coreclr/hosts)
    add_subdirectory(src/ildasm/unixcoreclrloader)
endif(CLR_CMAKE_PLATFORM_UNIX)

# Add this subdir. We install the headers for the jit.
add_subdirectory(src/pal/prebuilt/inc)

add_subdirectory(src/debug/debug-pal)

if(WIN32)
  add_subdirectory(src/gc/sample)
endif()

# Above projects do not build with these compile options
# All of the compiler options are specified in file compileoptions.cmake
# Do not add any new options here. They should be added in compileoptions.cmake
if(WIN32)
  add_compile_options(/FIWarningControl.h) # force include of WarningControl.h
  add_compile_options(/Zl) # omit default library name in .OBJ
endif(WIN32)

#------------------------------
# Add Test Directory
#------------------------------
if(CLR_CMAKE_BUILD_TESTS)
  add_subdirectory(tests)
endif(CLR_CMAKE_BUILD_TESTS)

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
include_directories("bin/obj")

if(FEATURE_STANDALONE_GC)
  add_definitions(-DFEATURE_STANDALONE_GC)
  add_subdirectory(src/gc)
endif(FEATURE_STANDALONE_GC)

if (CLR_CMAKE_PLATFORM_UNIX)
  include_directories("src/pal/inc")
  include_directories("src/pal/inc/rt")
  include_directories("src/pal/src/safecrt")
endif (CLR_CMAKE_PLATFORM_UNIX)

#------------------------------
# Add Product Directory
#------------------------------
add_subdirectory(src)

include(definitionsconsistencycheck.cmake)
