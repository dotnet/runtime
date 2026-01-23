# SPDX-License-Identifier: 0BSD

#############################################################################
#
# Optional file to be included by the top-level CMakeLists.txt to run tests
#
# The CMake rules for the tests are in this separate optional file so
# that it's trivial to just delete the whole "tests" directory and still
# get an otherwise normal CMake-based build. This way it's easy to ensure
# that nothing in the "tests" directory can affect the build process.
#
# Author: Lasse Collin
#
#############################################################################

include(CTest)

if(BUILD_TESTING)
    ################################
    # Windows Application Manifest #
    ################################

    # On Windows (but not on Cygwin or MSYS2) we want to add the
    # application manifest to the test programs so that they are
    # run in the same configuration as the programs that are installed.
    # The same object file can be used for all test programs.
    add_library(tests_w32res OBJECT)

    # CMake requires that even an object library must have at least once
    # source file. Give it a header file that results in no output files.
    target_sources(tests_w32res PRIVATE tests/tests.h)

    # The Ninja Generator requires setting the linker language since it
    # cannot guess the programming language of a header file.
    set_target_properties(tests_w32res PROPERTIES LINKER_LANGUAGE C)

    target_include_directories(tests_w32res PRIVATE src/common
        src/common
        src/liblzma/api
    )

    if(WIN32)
        target_sources(tests_w32res PRIVATE tests/tests_w32res.rc)
        set_source_files_properties(tests/tests_w32res.rc PROPERTIES
            OBJECT_DEPENDS "${W32RES_DEPENDENCIES}"
        )
    endif()

    #################
    # liblzma tests #
    #################

    set(LIBLZMA_TESTS
        test_bcj_exact_size
        test_block_header
        test_check
        test_filter_flags
        test_filter_str
        test_hardware
        test_index
        test_index_hash
        test_lzip_decoder
        test_memlimit
        test_stream_flags
        test_vli
    )

    # MicroLZMA encoder is needed for both encoder and decoder tests.
    # If MicroLZMA decoder is not configured but LZMA1 decoder is, then
    # test_microlzma will fail to compile because this configuration is
    # not possible in the Autotools build, so the test was not made to
    # support it since it would have required additional changes.
    if (XZ_MICROLZMA_ENCODER AND (XZ_MICROLZMA_DECODER
            OR NOT "lzma1" IN_LIST DECODERS))
        list(APPEND LIBLZMA_TESTS test_microlzma)
    endif()

    foreach(TEST IN LISTS LIBLZMA_TESTS)
        add_executable("${TEST}" "tests/${TEST}.c")

        target_include_directories("${TEST}" PRIVATE
            src/common
            src/liblzma/api
            src/liblzma
        )

        target_link_libraries("${TEST}" PRIVATE liblzma tests_w32res)

        # Put the test programs into their own subdirectory so they don't
        # pollute the top-level dir which might contain xz and xzdec.
        set_target_properties("${TEST}" PROPERTIES
            RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/tests_bin"
        )

        add_test(NAME "${TEST}"
                 COMMAND "${CMAKE_CURRENT_BINARY_DIR}/tests_bin/${TEST}"
        )

        # Set srcdir environment variable so that the tests find their
        # input files from the source tree.
        #
        # Set the return code for skipped tests to match Automake convention.
        set_tests_properties("${TEST}" PROPERTIES
            ENVIRONMENT "srcdir=${CMAKE_CURRENT_SOURCE_DIR}/tests"
            SKIP_RETURN_CODE 77
        )
    endforeach()


    ###########################
    # Command line tool tests #
    ###########################

    # Since the CMake-based build doesn't use config.h, the test scripts
    # cannot grep the contents of config.h to know which features have
    # been disabled. When config.h is missing, they assume that all
    # features are enabled. Thus, check if certain groups of features have
    # been disabled and then possibly skip some of the tests entirely instead
    # of letting them fail.
    set(SUPPORTED_FILTERS_SORTED "${SUPPORTED_FILTERS}")
    list(SORT SUPPORTED_FILTERS_SORTED)

    set(ENCODERS_SORTED "${XZ_ENCODERS}")
    list(SORT ENCODERS_SORTED)

    if("${ENCODERS_SORTED}" STREQUAL "${SUPPORTED_FILTERS_SORTED}")
        set(HAVE_ALL_ENCODERS ON)
    else()
        set(HAVE_ALL_ENCODERS OFF)
    endif()

    set(DECODERS_SORTED "${XZ_DECODERS}")
    list(SORT DECODERS_SORTED)

    if("${DECODERS_SORTED}" STREQUAL "${SUPPORTED_FILTERS_SORTED}")
        set(HAVE_ALL_DECODERS ON)
    else()
        set(HAVE_ALL_DECODERS OFF)
    endif()

    set(SUPPORTED_CHECKS_SORTED "${SUPPORTED_CHECKS}")
    list(SORT SUPPORTED_CHECKS_SORTED)

    set(XZ_CHECKS_SORTED "${XZ_CHECKS}")
    list(SORT XZ_CHECKS_SORTED)

    if("${SUPPORTED_CHECKS_SORTED}" STREQUAL "${XZ_CHECKS_SORTED}")
        set(HAVE_ALL_CHECKS ON)
    else()
        set(HAVE_ALL_CHECKS OFF)
    endif()

    # test_scripts.sh only needs LZMA2 decoder and CRC32.
    if(ENABLE_SCRIPTS)
        file(MAKE_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/test_scripts")

        add_test(NAME test_scripts.sh
            COMMAND "${CMAKE_CURRENT_SOURCE_DIR}/tests/test_scripts.sh" ".."
            WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/test_scripts"
        )

        set_tests_properties(test_scripts.sh PROPERTIES
            ENVIRONMENT "srcdir=${CMAKE_CURRENT_SOURCE_DIR}/tests"
            SKIP_RETURN_CODE 77
        )
    endif()

    # test_suffix.sh only needs LZMA2 encoder and decoder.
    if(UNIX AND HAVE_ENCODERS AND HAVE_DECODERS)
        file(MAKE_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/test_suffix")

        add_test(NAME test_suffix.sh
            COMMAND "${CMAKE_CURRENT_SOURCE_DIR}/tests/test_suffix.sh" ".."
            WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/test_suffix"
        )

        set_tests_properties(test_suffix.sh PROPERTIES
            SKIP_RETURN_CODE 77
        )
    endif()

    # The test_compress.sh based tests compress and decompress using different
    # filters so run it only if all encoders and decoders have been enabled.
    if(UNIX AND HAVE_ALL_ENCODERS AND HAVE_ALL_DECODERS)
        file(MAKE_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/test_compress")

        add_executable(create_compress_files tests/create_compress_files.c)
        target_link_libraries(create_compress_files PRIVATE tests_w32res)
        target_include_directories(create_compress_files PRIVATE src/common)
        set_target_properties(create_compress_files PROPERTIES
                              RUNTIME_OUTPUT_DIRECTORY test_compress)

        foreach(T compress_generated_abc
                  compress_generated_text
                  compress_generated_random)
            add_test(NAME "test_${T}"
                COMMAND "${CMAKE_CURRENT_SOURCE_DIR}/tests/test_compress.sh"
                        "${T}" ".."
                WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/test_compress"
            )

            set_tests_properties("test_${T}" PROPERTIES
                ENVIRONMENT "srcdir=${CMAKE_CURRENT_SOURCE_DIR}/tests"
                SKIP_RETURN_CODE 77
            )
        endforeach()
    endif()

    # test_files.sh decompresses files that use different filters and
    # check types so run it only if support for all of them has been enabled.
    if(UNIX AND HAVE_ALL_DECODERS AND HAVE_ALL_CHECKS AND XZ_LZIP_DECODER)
        # test_files.sh doesn't make any temporary files but it
        # must not be run at the top-level build directory because
        # it checks if ../config.h exists. We don't want to read
        # files outside the build directory!
        file(MAKE_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/test_files")

        add_test(NAME test_files.sh
            COMMAND "${CMAKE_CURRENT_SOURCE_DIR}/tests/test_files.sh" ".."
            WORKING_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/test_files"
        )

        set_tests_properties(test_files.sh PROPERTIES
            ENVIRONMENT "srcdir=${CMAKE_CURRENT_SOURCE_DIR}/tests"
            SKIP_RETURN_CODE 77
        )
    endif()
endif()
