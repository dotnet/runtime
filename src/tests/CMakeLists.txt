cmake_minimum_required(VERSION 3.6.2)

cmake_policy(SET CMP0042 NEW)
if (CMAKE_VERSION VERSION_GREATER 3.15 OR CMAKE_VERSION VERSION_EQUAL 3.15)
  cmake_policy(SET CMP0091 NEW)
endif()
project(Tests)

include(../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

include_directories("${CLR_SRC_NATIVE_DIR}")

# Add this subdir. We install the headers for the jit.
add_subdirectory(${CLR_REPO_ROOT_DIR}/src/coreclr/pal/prebuilt/inc ${CLR_ARTIFACTS_OBJ_DIR}/coreclr/pal/prebuilt/inc)

set(INC_PLATFORM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/Common/Platform)
if (CLR_CMAKE_TARGET_WIN32)
    add_definitions(-DWINDOWS)
endif()

# Compile options

if (CLR_CMAKE_HOST_WIN32)
    # 4100 - unreferenced formal parameter
    # 4244 - conversion from 'type1' to 'type2', possible loss of data
    # 4514 - unreferenced inline function has been removed
    # 4625 - copy constructor was implicitly defined as deleted because a base class copy constructor is inaccessible or deleted
    # 4626 - assignment operator was implicitly defined as deleted because a base class assignment operator is inaccessible or deleted
    # 4668 - 'symbol' is not defined as a preprocessor macro, replacing with '0' for 'directives'
    # 4710 - function not inlined
    # 4711 - 'function' selected for inline expansion
    # 4774 - format string expected in argument number is not a string literal
    # 4820 - bytes padding added after construct 'member_name'
    # 5025 - move assignment operator was implicitly defined as deleted
    # 5026 - move constructor was implicitly defined as deleted
    # 5027 - move assignment operator was implicitly defined as deleted
    # 5039 - pointer or reference to potentially throwing function passed to extern C function under -EHc. Undefined behavior may occur if this function throws an exception.
    add_compile_options(-wd4100 -wd4244 -wd4514 -wd4625 -wd4626 -wd4668 -wd4710 -wd4711 -wd4774 -wd4820 -wd5025 -wd5026 -wd5027 -wd5039)

    string(REPLACE "/EHsc" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
    add_compile_options(/EHa) # enable C++ EH (w/ SEH exceptions)
endif()

MACRO(SUBDIRLIST result curdir)
  FILE(GLOB children RELATIVE ${curdir} ${curdir}/*)
  SET(dirlist "")
  FOREACH(child ${children})
    IF(IS_DIRECTORY ${curdir}/${child})
        LIST(APPEND dirlist ${child})
    ENDIF()
  ENDFOREACH()
  SET(${result} ${dirlist})
ENDMACRO()

MACRO(ADDSUBDIR_REC  curdir)
    SUBDIRLIST(SUB_DIRS ${curdir})
    FOREACH(subdir ${SUB_DIRS})
        if(EXISTS "${curdir}/${subdir}/CMakeLists.txt")
           ADD_SUBDIRECTORY(${curdir}/${subdir})
        else()
           ADDSUBDIR_REC(${curdir}/${subdir})
        endif(EXISTS "${curdir}/${subdir}/CMakeLists.txt")
    ENDFOREACH()
ENDMACRO()

ADDSUBDIR_REC("${CMAKE_CURRENT_SOURCE_DIR}")
