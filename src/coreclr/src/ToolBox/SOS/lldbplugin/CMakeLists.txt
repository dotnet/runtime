project(sosplugin)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(ENABLE_LLDBPLUGIN ${CLR_CMAKE_PLATFORM_UNIX} CACHE BOOL "Enable building the SOS plugin for LLDB.")
set(REQUIRE_LLDBPLUGIN ${CLR_CMAKE_PLATFORM_LINUX} CACHE BOOL "Require building the SOS plugin for LLDB.")

set(WITH_LLDB_LIBS "" CACHE PATH "Path to LLDB libraries")
set(WITH_LLDB_INCLUDES "" CACHE PATH "Path to LLDB headers")

if(NOT ENABLE_LLDBPLUGIN)
    return()
endif()

# Check for LLDB library
find_library(LLDB NAMES lldb lldb-3.5 PATHS "${WITH_LLDB_LIBS}")
if(LLDB STREQUAL LLDB-NOTFOUND)
    if(REQUIRE_LLDBPLUGIN)
        message(FATAL_ERROR "Cannot find lldb or lldb-3.5. Try installing lldb-3.5-dev (or the appropriate package for your platform)")
    else()
        message(WARNING "Cannot find lldb or lldb-3.5. Try installing lldb-3.5-dev (or the appropriate package for your platform)")
    endif()
    return()
endif()

# Check for LLDB headers
find_path(LLDB_H "lldb/API/LLDB.h" PATHS "${WITH_LLDB_INCLUDES}")
if(LLDB_H STREQUAL LLDB_H-NOTFOUND)
    find_path(LLDB_H "lldb/API/LLDB.h" PATHS "/usr/lib/llvm-3.5/include")
    if(LLDB_H STREQUAL LLDB_H-NOTFOUND)
        if(REQUIRE_LLDBPLUGIN)
            message(FATAL_ERROR "Cannot find LLDB.h. Try installing lldb-3.5-dev (or the appropriate package for your platform)")
        else()
            message(WARNING "Cannot find LLDB.h Try installing lldb-3.5-dev (or the appropriate package for your platform)")
        endif()
        return()
    endif()
endif()

add_compile_options(-Wno-delete-non-virtual-dtor)

include_directories(inc)
include_directories("${LLDB_H}")
set(SOURCES
    sosplugin.cpp
    soscommand.cpp
    debugclient.cpp
)

add_library(sosplugin SHARED ${SOURCES})
add_dependencies(sosplugin sos)

if (CLR_CMAKE_PLATFORM_UNIX)
    target_link_libraries(sosplugin ${LLDB})
endif()

# add the install targets
install (TARGETS sosplugin DESTINATION .)
