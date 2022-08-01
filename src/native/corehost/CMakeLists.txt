cmake_minimum_required(VERSION 3.6.2)

if (CMAKE_VERSION VERSION_GREATER 3.15 OR CMAKE_VERSION VERSION_EQUAL 3.15)
    cmake_policy(SET CMP0091 NEW)
endif()

project(corehost)

include(../../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

if (MSVC)
    # Host components don't try to handle asynchronous exceptions
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:/EHsc>)

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

add_subdirectory(hostcommon)
add_subdirectory(fxr)
add_subdirectory(hostpolicy)

add_subdirectory(apphost)
add_subdirectory(dotnet)
add_subdirectory(nethost)
add_subdirectory(test)

if (NOT RUNTIME_FLAVOR STREQUAL Mono)
    if(CLR_CMAKE_TARGET_WIN32)
        add_subdirectory(comhost)
        add_subdirectory(ijwhost)
    endif()
endif()
