# Require at least version 2.8.12 of CMake
cmake_minimum_required(VERSION 2.8.12)

if (CMAKE_CONFIGURATION_TYPES) # multi-configuration generator?
    set(CMAKE_CONFIGURATION_TYPES "Debug;Checked;Release;RelWithDebInfo" CACHE STRING "" FORCE)
endif (CMAKE_CONFIGURATION_TYPES)
set(CMAKE_C_FLAGS_CHECKED "")
set(CMAKE_CXX_FLAGS_CHECKED "")
set(CMAKE_EXE_LINKER_FLAGS_CHECKED "")
set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "")

set(CLR_CMAKE_TARGET_ARCH ${CLR_CMAKE_HOST_ARCH})

set(INC_PLATFORM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/src/Common/Platform)
if (WIN32)
    add_definitions(-DWINDOWS=1)

    if (DEFINED ENV{__ToolsetDir})
        # Hack for private Tool Set
        # CMAKE_CXX_COMPILER will default to the compiler installed with
        # Visual studio. Overwrite it to the compiler on the path.

        find_program(PATH_CXX_COMPILER cl)
        set(CMAKE_CXX_COMPILER ${PATH_CXX_COMPILER})

        message("Overwriting the CMAKE_CXX_COMPILER.")
        message("CMAKE_CXX_COMPILER found:${CMAKE_CXX_COMPILER}")
      
        # Temporary until cmake has VS generators for hacky toolsets [arm64]
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /machine:${CLR_CMAKE_TARGET_ARCH}")
        set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /machine:${CLR_CMAKE_TARGET_ARCH}")
        set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /machine:${CLR_CMAKE_TARGET_ARCH}")
    endif()
endif()

# Compile options

if (WIN32)
    # 4100 - unreferenced formal parameter
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
    add_compile_options(-wd4100 -wd4514 -wd4625 -wd4626 -wd4668 -wd4710 -wd4711 -wd4774 -wd4820 -wd5025 -wd5026 -wd5027 -wd5039)
    set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /EHsc")
endif()

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

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

ADDSUBDIR_REC(${CMAKE_CURRENT_SOURCE_DIR})
