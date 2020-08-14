cmake_minimum_required(VERSION 3.6.2)

project(corehost)

include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

if(MSVC)
    add_compile_options(/W1)

    # Host components don't try to handle asynchronous exceptions
    add_compile_options(/EHsc)
elseif (CMAKE_CXX_COMPILER_ID MATCHES GNU)
    # Prevents libc from calling pthread_cond_destroy on static objects in
    # dlopen()'ed library which we dlclose() in pal::unload_library.
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-fno-use-cxa-atexit>)
endif()

add_subdirectory(cli)
