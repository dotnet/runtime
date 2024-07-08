cmake_minimum_required(VERSION 3.20)
project(corehost)

include(../../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

set(COREHOST_ROOT_DIR "${CMAKE_CURRENT_SOURCE_DIR}")

if (MSVC)
    # Host components don't try to handle asynchronous exceptions
    set_property(DIRECTORY PROPERTY CLR_EH_OPTION /EHsc)

    # Explicitly re-enable warnings about declaration hiding (currently disabled by configurecompiler.cmake)
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4456>) # declaration of 'identifier' hides previous local declaration
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4457>) # declaration of 'identifier' hides function parameter
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4458>) # declaration of 'identifier' hides class member
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4459>) # declaration of 'identifier' hides global declaration
elseif (CMAKE_CXX_COMPILER_ID MATCHES GNU)
    # Prevents libc from calling pthread_cond_destroy on static objects in
    # dlopen()'ed library which we dlclose() in pal::unload_library.
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-fno-use-cxa-atexit>)
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(${CMAKE_CURRENT_BINARY_DIR})
include_directories(${CMAKE_CURRENT_SOURCE_DIR})
include_directories(${CLR_ARTIFACTS_OBJ_DIR}) # Generated version files

if (NOT ${CLR_SINGLE_FILE_HOST_ONLY})
    if("${CLI_CMAKE_PKG_RID}" STREQUAL "")
        message(FATAL_ERROR "A minimum supported package rid is not specified (ex: win7-x86 or ubuntu.14.04-x64, osx.10.12-x64, rhel.7-x64)")
    endif()
    if("${CLI_CMAKE_COMMIT_HASH}" STREQUAL "")
        message(FATAL_ERROR "Commit hash needs to be specified to build the host")
    endif()
endif()

if("${CLI_CMAKE_FALLBACK_OS}" STREQUAL "")
    message(FATAL_ERROR "Fallback rid needs to be specified to build the host")
endif()

if("${CLI_CMAKE_FALLBACK_OS}" STREQUAL "${CLR_CMAKE_TARGET_OS}")
    add_compile_definitions(FALLBACK_OS_IS_SAME_AS_TARGET_OS)
endif()

# Find support libraries that we need if they're present on the system.
find_library(PTHREAD_LIB pthread)

# Prefer libatomic.a over libatomic.so.
set(_current_CMAKE_FIND_LIBRARY_SUFFIXES ${CMAKE_FIND_LIBRARY_SUFFIXES})
list(PREPEND CMAKE_FIND_LIBRARY_SUFFIXES .a)
find_library(ATOMIC_SUPPORT_LIB atomic)
set(CMAKE_FIND_LIBRARY_SUFFIXES ${_current_CMAKE_FIND_LIBRARY_SUFFIXES})
unset(_current_CMAKE_FIND_LIBRARY_SUFFIXES)

configure_file(configure.h.in ${CMAKE_CURRENT_BINARY_DIR}/configure.h)

# add_version_info_to_target(targetName [resourceDirName])
function(add_version_info_to_target targetName)
    set(RESOURCE_INCLUDE_DIR ${targetName})
    if (${ARGC} GREATER 1)
        set(RESOURCE_INCLUDE_DIR ${ARGV1})
    endif()

    if (CLR_CMAKE_TARGET_WIN32)
        target_sources(${targetName} PRIVATE ${CMAKE_CURRENT_FUNCTION_LIST_DIR}/native.rc)
        set_property(SOURCE ${CMAKE_CURRENT_FUNCTION_LIST_DIR}/native.rc
            TARGET_DIRECTORY ${targetName}
            APPEND PROPERTY INCLUDE_DIRECTORIES
            ${CLI_CMAKE_RESOURCE_DIR}/${RESOURCE_INCLUDE_DIR})
    else()
        target_sources(${targetName} PRIVATE ${VERSION_FILE_PATH})
    endif()
endfunction()

# This is required to map a symbol reference to a matching definition local to the module (.so)
# containing the reference instead of using definitions from other modules.
if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_SUNOS)
    add_link_options(LINKER:-Bsymbolic)
endif()

add_library(fxr_resolver INTERFACE)
target_sources(fxr_resolver INTERFACE fxr_resolver.cpp)
target_include_directories(fxr_resolver INTERFACE fxr)

if ((NOT DEFINED CLR_CMAKE_USE_SYSTEM_RAPIDJSON) OR (NOT CLR_CMAKE_USE_SYSTEM_RAPIDJSON))
    include_directories(${CLR_SRC_NATIVE_DIR}/external/)
endif()

add_subdirectory(hostcommon)
add_subdirectory(hostmisc)
add_subdirectory(fxr)
add_subdirectory(hostpolicy)

add_subdirectory(apphost)
add_subdirectory(dotnet)
add_subdirectory(nethost)
add_subdirectory(test)

# If there's a dynamic ASAN runtime, then install it in the directories where we put our executables.
if (NOT "${ASAN_RUNTIME}" STREQUAL "")
    install(FILES ${ASAN_RUNTIME} DESTINATION corehost)
    install(FILES ${ASAN_RUNTIME} DESTINATION corehost_test)
endif()

if (NOT RUNTIME_FLAVOR STREQUAL Mono)
    if(CLR_CMAKE_TARGET_WIN32)
        add_subdirectory(comhost)
        add_subdirectory(ijwhost)
    endif()
endif()
