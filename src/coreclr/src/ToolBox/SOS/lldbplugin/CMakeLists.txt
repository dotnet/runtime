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

set(ENABLE_LLDBPLUGIN ${CLR_CMAKE_PLATFORM_UNIX} CACHE BOOL "Enable building the SOS plugin for LLDB.")
set(REQUIRE_LLDBPLUGIN ${CLR_CMAKE_PLATFORM_LINUX} CACHE BOOL "Require building the SOS plugin for LLDB.")

if(SKIP_LLDBPLUGIN)
    SET(REQUIRE_LLDBPLUGIN false)
endif()

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
    add_definitions(-D_TARGET_AMD64_=1)
    add_definitions(-DDBG_TARGET_64BIT=1)
    add_definitions(-DDBG_TARGET_AMD64=1)
    add_definitions(-DDBG_TARGET_WIN64=1)
    add_definitions(-DBIT64)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
    add_definitions(-D_TARGET_X86_=1)
    add_definitions(-DDBG_TARGET_32BIT=1)
    add_definitions(-DDBG_TARGET_X86=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
    add_definitions(-D_TARGET_ARM_=1)
    add_definitions(-DDBG_TARGET_32BIT=1)
    add_definitions(-DDBG_TARGET_ARM=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    add_definitions(-D_TARGET_ARM64_=1)
    add_definitions(-DDBG_TARGET_64BIT=1)
    add_definitions(-DDBG_TARGET_ARM64=1)
    add_definitions(-DDBG_TARGET_WIN64=1)
    add_definitions(-DBIT64)
    SET(REQUIRE_LLDBPLUGIN false)
endif()

set(LLVM_HOST_DIR "$ENV{LLVM_HOME}")
set(WITH_LLDB_LIBS "${LLVM_HOST_DIR}/lib" CACHE PATH "Path to LLDB libraries")
set(WITH_LLDB_INCLUDES "${LLVM_HOST_DIR}/include" CACHE PATH "Path to LLDB headers")

if(NOT ENABLE_LLDBPLUGIN)
    return()
endif()

if (CLR_CMAKE_PLATFORM_DARWIN)
    # Check for LLDB library
    find_library(LLDB NAMES LLDB lldb lldb-6.0 lldb-5.0 lldb-4.0 lldb-3.9 lldb-3.8 lldb-3.7 lldb-3.6 lldb-3.5 PATHS "${WITH_LLDB_LIBS}" PATH_SUFFIXES llvm NO_DEFAULT_PATH)
    find_library(LLDB NAMES LLDB lldb lldb-6.0 lldb-5.0 lldb-4.0 lldb-3.9 lldb-3.8 lldb-3.7 lldb-3.6 lldb-3.5 PATH_SUFFIXES llvm)
    if(LLDB STREQUAL LLDB-NOTFOUND)
        if(REQUIRE_LLDBPLUGIN)
            set(MESSAGE_MODE FATAL_ERROR)
        else()
            set(MESSAGE_MODE WARNING)
        endif()
        message(${MESSAGE_MODE} "Cannot find lldb-3.5, lldb-3.6, lldb-3.8, lldb-3.9, lldb-4.0, lldb-5.0 or lldb-6.0. Try installing liblldb-3.9-dev (or the appropriate package for your platform). You may need to set LLVM_HOME if the build still can't find it.")

        return()
    endif()

    message(STATUS "LLDB: ${LLDB}")
endif()

# Check for LLDB headers
# Multiple versions of LLDB can install side-by-side, so we need to check for lldb in various locations.
# If the file in a directory is found the result is stored in the variable and the search will not be repeated unless the variable is cleared.
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "${WITH_LLDB_INCLUDES}" NO_DEFAULT_PATH)
find_path(LLDB_H "lldb/API/LLDB.h")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-6.0/include")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-5.0/include")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-4.0/include")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.9/include")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.8/include")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.7/include")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.6/include")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.5/include")
#FreeBSD
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/local/llvm39/include")
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/local/llvm38/include")

if(LLDB_H STREQUAL LLDB_H-NOTFOUND)
    if(REQUIRE_LLDBPLUGIN)
        set(MESSAGE_MODE FATAL_ERROR)
    else()
        set(MESSAGE_MODE WARNING)
    endif()
    message(${MESSAGE_MODE} "Cannot find LLDB.h Try installing lldb-3.9-dev (or the appropriate package for your platform). You may need to set LLVM_HOME if the build still can't find it.")
    return()
endif()

message(STATUS "LLDB_H: ${LLDB_H}")

add_compile_options(-Wno-delete-non-virtual-dtor)

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
   target_link_libraries(sosplugin ${LLDB})
endif()

# add the install targets
install_clr(sosplugin)
