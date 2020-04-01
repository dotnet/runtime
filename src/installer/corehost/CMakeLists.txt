cmake_minimum_required(VERSION 3.14.2)

project(corehost)

include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

if(MSVC)
    add_compile_options(/W1)

    # Host components don't try to handle asynchronous exceptions
    add_compile_options(/EHsc)
endif()

add_subdirectory(cli)
