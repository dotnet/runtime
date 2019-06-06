project(sosplugin)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Set the RPATH of sos so that it can find dependencies without needing to set LD_LIBRARY_PATH
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if (CORECLR_SET_RPATH)
  set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
  if(CLR_CMAKE_PLATFORM_DARWIN)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_PLATFORM_DARWIN)
endif (CORECLR_SET_RPATH)

add_definitions(-DPAL_STDCPP_COMPAT)

if (CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD)
 set(PLATFORM_SUPPORTS_LLDB_PLUGIN true)
endif()

set(ENABLE_LLDBPLUGIN ${PLATFORM_SUPPORTS_LLDB_PLUGIN} CACHE BOOL "Enable building the SOS plugin for LLDB.")
set(REQUIRE_LLDBPLUGIN ${PLATFORM_SUPPORTS_LLDB_PLUGIN} CACHE BOOL "Require building the SOS plugin for LLDB.")

if(SKIP_LLDBPLUGIN)
    SET(REQUIRE_LLDBPLUGIN false)
endif()

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
    add_definitions(-D_TARGET_AMD64_)
    add_definitions(-DDBG_TARGET_64BIT)
    add_definitions(-DDBG_TARGET_AMD64)
    add_definitions(-DDBG_TARGET_WIN64)
    add_definitions(-DBIT64)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
    add_definitions(-D_TARGET_X86_)
    add_definitions(-DDBG_TARGET_32BIT)
    add_definitions(-DDBG_TARGET_X86)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
    add_definitions(-D_TARGET_ARM_)
    add_definitions(-DDBG_TARGET_32BIT)
    add_definitions(-DDBG_TARGET_ARM)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    add_definitions(-D_TARGET_ARM64_)
    add_definitions(-DDBG_TARGET_64BIT)
    add_definitions(-DDBG_TARGET_ARM64)
    add_definitions(-DDBG_TARGET_WIN64)
    add_definitions(-DBIT64)
    SET(REQUIRE_LLDBPLUGIN false)
endif()

if(NOT $ENV{LLVM_HOME} STREQUAL "")
    set(LLDB_INCLUDE_DIR "$ENV{LLVM_HOME}/include")
    set(LLDB_LIB_DIR "$ENV{LLVM_HOME}/lib")
else()
    if(NOT $ENV{LLDB_INCLUDE_DIR} STREQUAL "")
       set(LLDB_INCLUDE_DIR "$ENV{LLDB_INCLUDE_DIR}")
    endif()
    if(NOT $ENV{LLDB_LIB_DIR} STREQUAL "")
       set(LLDB_LIB_DIR "$ENV{LLDB_LIB_DIR}")
    endif()
endif()

if(NOT ENABLE_LLDBPLUGIN)
    return()
endif()

if(NOT $ENV{LLDB_LIB} STREQUAL "")
    set(LLDB_LIB "$ENV{LLDB_LIB}")
else()
    # Check for LLDB library
    if(CLR_CMAKE_PLATFORM_DARWIN)
        find_library(LLDB_LIB NAMES LLDB lldb lldb-7 lldb-6.0 lldb-5.0 lldb-4.0 lldb-3.9 lldb-3.8 lldb-3.7 lldb-3.6 lldb-3.5 PATHS "${LLDB_LIB_DIR}" PATH_SUFFIXES llvm NO_DEFAULT_PATH)
        find_library(LLDB_LIB NAMES LLDB lldb lldb-7 lldb-6.0 lldb-5.0 lldb-4.0 lldb-3.9 lldb-3.8 lldb-3.7 lldb-3.6 lldb-3.5 PATH_SUFFIXES llvm)
        if(LLDB_LIB STREQUAL LLDB_LIB-NOTFOUND)
            if(REQUIRE_LLDBPLUGIN)
                set(MESSAGE_MODE FATAL_ERROR)
            else()
                set(MESSAGE_MODE WARNING)
            endif()
            message(${MESSAGE_MODE} "Cannot find lldb library. Try installing Xcode. You may need to set LLVM_HOME, LLDB_LIB_DIR or LLDB_LIB if the build still can't find it.")
            return()
        endif()
    endif()
endif()

message(STATUS "LLDB_LIB: ${LLDB_LIB}")

if(NOT $ENV{LLDB_H} STREQUAL "")
    set(LLDB_H "$ENV{LLDB_H}")
else()
    # Check for LLDB headers
    # Multiple versions of LLDB can install side-by-side, so we need to check for lldb in various locations.
    # If the file in a directory is found the result is stored in the variable and the search will not be repeated unless the variable is cleared.
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "${LLDB_INCLUDE_DIR}" NO_DEFAULT_PATH)
    find_path(LLDB_H "lldb/API/LLDB.h")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-7/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-6.0/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-5.0/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-4.0/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.9/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.8/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.7/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.6/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.5/include")
    #FreeBSD
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/local/llvm70/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/local/llvm60/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/local/llvm50/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/local/llvm40/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/local/llvm39/include")
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/local/llvm38/include")

    if(LLDB_H STREQUAL LLDB_H-NOTFOUND)
        if(REQUIRE_LLDBPLUGIN)
            set(MESSAGE_MODE FATAL_ERROR)
        else()
            set(MESSAGE_MODE WARNING)
        endif()
        message(${MESSAGE_MODE} "Cannot find LLDB.h Try installing lldb-3.9-dev (or the appropriate package for your platform). You may need to set LLVM_HOME or LLDB_INCLUDE_DIR if the build still can't find it.")
        return()
    endif()
endif()

message(STATUS "LLDB_H: ${LLDB_H}")

add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-delete-non-virtual-dtor>)

include_directories(inc)
include_directories("${LLDB_H}")
include_directories(${CLR_DIR}/src/debug/inc)
include_directories(${CLR_DIR}/src/inc)
include_directories(${CLR_DIR}/src/pal/inc)
include_directories(${CLR_DIR}/src/pal/inc/rt)

set(SOURCES
    sosplugin.cpp
    soscommand.cpp
    setclrpathcommand.cpp
    setsostidcommand.cpp
    services.cpp
)

_add_library(sosplugin SHARED ${SOURCES})
add_dependencies(sosplugin sos)

if (CLR_CMAKE_PLATFORM_DARWIN)
   target_link_libraries(sosplugin ${LLDB_LIB})
endif()

# add the install targets
install_clr(sosplugin)
