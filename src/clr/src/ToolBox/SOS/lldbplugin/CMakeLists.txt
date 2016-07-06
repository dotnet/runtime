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

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
    add_definitions(-D_TARGET_AMD64_=1)
    add_definitions(-DDBG_TARGET_64BIT=1)
    add_definitions(-DDBG_TARGET_AMD64=1)
    add_definitions(-DDBG_TARGET_WIN64=1)
    add_definitions(-DBIT64)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
    add_definitions(-D_TARGET_ARM_=1)
    add_definitions(-DDBG_TARGET_32BIT=1)
    add_definitions(-DDBG_TARGET_ARM=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    message(WARNING "lldb is not supported on linux/aarch64 yet")
    return()
endif()

set(ENABLE_LLDBPLUGIN ${CLR_CMAKE_PLATFORM_UNIX} CACHE BOOL "Enable building the SOS plugin for LLDB.")
set(REQUIRE_LLDBPLUGIN ${CLR_CMAKE_PLATFORM_LINUX} CACHE BOOL "Require building the SOS plugin for LLDB.")

set(WITH_LLDB_LIBS "" CACHE PATH "Path to LLDB libraries")
set(WITH_LLDB_INCLUDES "" CACHE PATH "Path to LLDB headers")

if(NOT ENABLE_LLDBPLUGIN)
    return()
endif()

# Check for LLDB library
find_library(LLDB NAMES LLDB lldb lldb-3.8 lldb-3.6 lldb-3.5 PATHS "${WITH_LLDB_LIBS}" PATH_SUFFIXES llvm NO_DEFAULT_PATH)
find_library(LLDB NAMES LLDB lldb lldb-3.8 lldb-3.6 lldb-3.5 PATH_SUFFIXES llvm)
if(LLDB STREQUAL LLDB-NOTFOUND)
    if(REQUIRE_LLDBPLUGIN)
        message(FATAL_ERROR "Cannot find lldb-3.5, lldb-3.6 or lldb-3.8. Try installing lldb-3.6-dev (or the appropriate package for your platform)")
    else()
        message(WARNING "Cannot find lldb-3.5,lldb-3.6 or lldb-3.8. Try installing lldb-3.6-dev (or the appropriate package for your platform)")
    endif()
    return()
endif()

message(STATUS "LLDB: ${LLDB}")

# Check for LLDB headers

find_path(LLDB_H "lldb/API/LLDB.h" PATHS "${WITH_LLDB_INCLUDES}" NO_DEFAULT_PATH)
find_path(LLDB_H "lldb/API/LLDB.h")
if(LLDB_H STREQUAL LLDB_H-NOTFOUND)
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.8/include")
    if(LLDB_H STREQUAL LLDB_H-NOTFOUND)
        find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.6/include")
        if(LLDB_H STREQUAL LLDB_H-NOTFOUND)
            find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.5/include")
            if(LLDB_H STREQUAL LLDB_H-NOTFOUND)
                if(REQUIRE_LLDBPLUGIN)
                    message(FATAL_ERROR "Cannot find LLDB.h. Try installing lldb-3.6-dev (or the appropriate package for your platform)")
                else()
                    message(WARNING "Cannot find LLDB.h Try installing lldb-3.6-dev (or the appropriate package for your platform)")
                endif()
                return()
            endif()
        endif()
    endif()
endif()

message(STATUS "LLDB_H: ${LLDB_H}")

add_compile_options(-Wno-delete-non-virtual-dtor)

include_directories(inc)
include_directories("${LLDB_H}")
include_directories(${CLR_DIR}/src/debug/inc)
include_directories(${CLR_DIR}/src/inc)
include_directories(${CLR_DIR}/src/coreclr/hosts/inc)
include_directories(${CLR_DIR}/src/coreclr/hosts/unixcoreruncommon)

set(SOURCES
    sosplugin.cpp
    soscommand.cpp
    setclrpathcommand.cpp
    setsostidcommand.cpp
    services.cpp
)

if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
    list(APPEND SOURCES
        coreruncommand.cpp
        ${CLR_DIR}/src/coreclr/hosts/unixcorerun/corerun.cpp
        ${CLR_DIR}/src/coreclr/hosts/unixcoreruncommon/coreruncommon.cpp
    )
endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)

_add_library(sosplugin SHARED ${SOURCES})
add_dependencies(sosplugin sos)

if (CLR_CMAKE_PLATFORM_UNIX)
    target_link_libraries(sosplugin ${LLDB})
endif()

# add the install targets
install_clr(sosplugin)
