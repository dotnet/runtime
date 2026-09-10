cmake_minimum_required(VERSION 3.26)

project(Tests)

include(../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

include_directories("${CLR_SRC_NATIVE_DIR}")

add_subdirectory(${CLR_SRC_NATIVE_DIR}/minipal minipal)
list(APPEND LINK_LIBRARIES_ADDITIONAL PRIVATE minipal)

# Add this subdir. We install the headers for the jit.
add_subdirectory(${CLR_REPO_ROOT_DIR}/src/coreclr/pal/prebuilt/inc ${CLR_ARTIFACTS_OBJ_DIR}/coreclr/pal/prebuilt/inc)

set(INC_PLATFORM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/Common/Platform)
if (CLR_CMAKE_TARGET_WIN32)
    add_definitions(-DWINDOWS)
endif()

# Compile options

if (CLR_CMAKE_HOST_WIN32)
    # 4100 - unreferenced formal parameter
    # 4242 - conversion from 'type1' to 'type2', possible loss of data
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
    add_compile_options(-wd4100 -wd4242 -wd4244 -wd4514 -wd4625 -wd4626 -wd4668 -wd4710 -wd4711 -wd4774 -wd4820 -wd5025 -wd5026 -wd5027 -wd5039)

    set_property(DIRECTORY PROPERTY CLR_EH_OPTION /EHa) # enable C++ EH (w/ SEH exceptions)
endif()

if(CLR_CMAKE_TARGET_ARCH_WASM)
    # WebAssembly has no dynamic loading, so a test's native code is linked into a test-specific
    # corerun instead (see src/tests/Common/CLRTest.WasmCorerun.targets).
    set(TEST_LIB_TYPE STATIC)

    # That corerun is only produced for browser CoreCLR. CLR_CMAKE_TARGET_ARCH_WASM is also true
    # for wasi, and the same tree is configured for the mono browser leg, so gate on both.
    string(TOLOWER "${CMAKE_BUILD_RUNTIME_FLAVOR}" _TESTS_RUNTIME_FLAVOR)
    if(CLR_CMAKE_TARGET_BROWSER AND _TESTS_RUNTIME_FLAVOR STREQUAL "coreclr")
        set(_TESTS_WASM_BUILDS_CORERUN 1)
    endif()
else()
    set(TEST_LIB_TYPE SHARED)
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

if(CLR_CMAKE_TARGET_ARCH_WASM AND NOT _TESTS_WASM_BUILDS_CORERUN)
    # Nothing links the native test libraries here, so walking the whole tree would cost a full
    # compile of every one of them for nothing.
    add_subdirectory(JIT/interpreter)
else()
    ADDSUBDIR_REC("${CMAKE_CURRENT_SOURCE_DIR}")
endif()
