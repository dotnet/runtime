cmake_minimum_required(VERSION 3.5.1)

if(CMAKE_C_COMPILER_ID MATCHES "Clang")
    enable_language(CXX)

    if(DEFINED ENV{LIB_FUZZING_ENGINE})
        set(FUZZING_ENGINE $ENV{LIB_FUZZING_ENGINE})
        set(FUZZING_ENGINE_FOUND ON)
    else()
        find_library(FUZZING_ENGINE "FuzzingEngine")
    endif()
endif()

set(FUZZERS
    fuzzer_checksum
    fuzzer_compress
    fuzzer_example_small
    fuzzer_example_large
    fuzzer_example_flush
    fuzzer_example_dict
    )

if(WITH_GZFILEOP)
    list(APPEND FUZZERS fuzzer_minigzip)
endif()

foreach(FUZZER ${FUZZERS})
    add_executable(${FUZZER} ${FUZZER}.c)

    if(NOT FUZZING_ENGINE_FOUND)
        target_sources(${FUZZER} PRIVATE standalone_fuzz_target_runner.c)
    endif()

    if(NOT DEFINED BUILD_SHARED_LIBS)
        target_link_libraries(${FUZZER} zlibstatic)
    else()
        target_link_libraries(${FUZZER} zlib)
    endif()

    if(FUZZING_ENGINE_FOUND)
        target_link_libraries(${FUZZER} ${FUZZING_ENGINE})
    endif()

    if(ZLIB_ENABLE_TESTS)
        file(GLOB FUZZER_TEST_FILES ${PROJECT_SOURCE_DIR}/*)
        set(FUZZER_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:${FUZZER}> ${FUZZER_TEST_FILES})
        add_test(NAME ${FUZZER} COMMAND ${FUZZER_COMMAND})
    endif()
endforeach()
