# compress-and-verify.cmake -- Runs a test against an input file to make sure that the specified
#   targets are able to to compress and then decompress successfully. Optionally verify
#   the results with gzip. Output files are generated with unique names to prevent parallel
#   tests from corrupting one another. Default target arguments are compatible with minigzip.

# Copyright (C) 2021 Nathan Moinvaziri
# Licensed under the Zlib license, see LICENSE.md for details

# that test a specific input file for compression or decompression.

# Required Variables
#   INPUT                   - Input file to test
#   TARGET or               - Command to run for both compress and decompress
#     COMPRESS_TARGET and   - Command to run to compress input file
#     DECOMPRESS_TARGET     - Command to run to decompress output file

# Optional Variables
#   TEST_NAME               - Name of test to use when constructing output file paths
#   COMPRESS_ARGS           - Arguments to pass for compress command (default: -c -k)
#   DECOMPRESS_ARGS         - Arguments to pass to decompress command (default: -d -c)

#   GZIP_VERIFY             - Verify that gzip can decompress the COMPRESS_TARGET output and
#                             verify that DECOMPRESS_TARGET can decompress gzip output of INPUT
#   COMPARE                 - Verify decompressed output is the same as input
#   FILEMODE                - Pass data to/from (de)compressor using files instead of stdin/stdout
#   SUCCESS_EXIT            - List of successful exit codes (default: 0, ie: 0;1)

if(TARGET)
    set(COMPRESS_TARGET ${TARGET})
    set(DECOMPRESS_TARGET ${TARGET})
endif()

if(NOT DEFINED INPUT OR NOT DEFINED COMPRESS_TARGET OR NOT DEFINED DECOMPRESS_TARGET)
    message(FATAL_ERROR "Compress test arguments missing")
endif()

# Set default values
if(NOT DEFINED COMPARE)
    set(COMPARE ON)
endif()
if(NOT DEFINED FILEMODE)
    set(FILEMODE OFF)
endif()
if(NOT DEFINED COMPRESS_ARGS)
    set(COMPRESS_ARGS -3 -k)
    if(NOT FILEMODE)
        list(APPEND COMPRESS_ARGS -c)
    endif()
endif()
if(NOT DEFINED DECOMPRESS_ARGS)
    set(DECOMPRESS_ARGS -d -k)
    if(NOT FILEMODE)
        list(APPEND DECOMPRESS_ARGS -c)
    endif()
endif()
if(NOT DEFINED GZIP_VERIFY)
    set(GZIP_VERIFY ON)
endif()
if(NOT DEFINED SUCCESS_EXIT)
    set(SUCCESS_EXIT 0)
endif()

# Use test name from input file name
if(NOT DEFINED TEST_NAME)
    get_filename_component(TEST_NAME "${INPUT}" NAME)
endif()

# Generate unique output path so multiple tests can be executed at the same time
string(RANDOM LENGTH 6 UNIQUE_ID)
string(REPLACE "." "-" TEST_NAME "${TEST_NAME}")
set(OUTPUT_BASE "${CMAKE_CURRENT_BINARY_DIR}/Testing/Temporary/${TEST_NAME}-${UNIQUE_ID}")

# Ensure directory exists for output files
get_filename_component(OUTPUT_DIR "${OUTPUT_BASE}" DIRECTORY)
file(MAKE_DIRECTORY "${OUTPUT_DIR}")

# Cleanup temporary files
macro(cleanup_always)
    file(GLOB TEMP_FILES ${OUTPUT_BASE}*)
    file(REMOVE ${TEMP_FILES})
endmacro()
# Clean up temporary files if not on CI
macro(cleanup)
    if(NOT DEFINED ENV{CI})
        cleanup_always()
    endif()
endmacro()

# Show differences between two files
macro(diff src1 src2)
    find_program(XXD xxd)
    if(XXD)
        find_program(DIFF diff)
        if(DIFF)
            set(XXD_COMMAND ${XXD} ${src1} ${src1}.hex)
            execute_process(COMMAND ${XXD_COMMAND})
            set(XXD_COMMAND ${XXD} ${src2} ${src2}.hex)
            execute_process(COMMAND ${XXD_COMMAND})

            set(DIFF_COMMAND ${DIFF} -u ${src1}.hex ${src2}.hex)
            execute_process(COMMAND ${DIFF_COMMAND}
                OUTPUT_FILE ${src2}.diff)

            file(READ ${src2}.diff DIFF_OUTPUT)
            message(STATUS "Diff:\n${DIFF_OUTPUT}")

            if(NOT DEFINED ENV{CI})
                file(REMOVE ${src1}.hex ${src2}.hex ${src2}.diff)
            endif()
        endif()
    endif()
endmacro()


macro(exec_streams tcmd tsrc tdst)
    execute_process(COMMAND ${CMAKE_COMMAND}
        "-DCOMMAND=${tcmd}"
        -DINPUT=${tsrc}
        -DOUTPUT=${tdst}
        "-DSUCCESS_EXIT=${SUCCESS_EXIT}"
        -P ${CMAKE_CURRENT_LIST_DIR}/run-and-redirect.cmake
        RESULT_VARIABLE CMD_RESULT)
endmacro()

macro(exec_files tcmd tsrc)
    execute_process(COMMAND
        ${tcmd} ${tsrc}
        RESULT_VARIABLE CMD_RESULT)
endmacro()

# Compress input file
if(NOT EXISTS ${INPUT})
    message(FATAL_ERROR "Cannot find compress input: ${INPUT}")
endif()

set(COMPRESS_COMMAND ${COMPRESS_TARGET} ${COMPRESS_ARGS})

set(INPUT_FILE ${OUTPUT_BASE})

# Make CMake copy and rename file in one operation
# The copied file permissions is standard 644 (-rw-r--r--)
if(NOT CMAKE_VERSION VERSION_LESS "3.19")
    set(CONFIGURE_NO_SOURCE_PERMISSIONS NO_SOURCE_PERMISSIONS)
endif()
configure_file(${INPUT} ${INPUT_FILE} COPYONLY ${CONFIGURE_NO_SOURCE_PERMISSIONS})

message(STATUS "Compress ${COMPRESS_COMMAND}")
message(STATUS "  Source file: ${INPUT}")
message(STATUS "  Compression input file: ${INPUT_FILE}")
message(STATUS "  Output: ${OUTPUT_BASE}.gz")

if(FILEMODE)
    exec_files("${COMPRESS_COMMAND}" "${INPUT_FILE}")
else()
    exec_streams("${COMPRESS_COMMAND}" "${INPUT_FILE}" "${OUTPUT_BASE}.gz")
endif()

if(CMD_RESULT)
    cleanup()
    message(FATAL_ERROR "Compress failed: ${CMD_RESULT}")
endif()

# Decompress output
if(NOT EXISTS ${OUTPUT_BASE}.gz)
    cleanup()
    message(FATAL_ERROR "Cannot find decompress input: ${OUTPUT_BASE}.gz")
endif()

set(DECOMPRESS_COMMAND ${DECOMPRESS_TARGET} ${DECOMPRESS_ARGS})

message(STATUS "Decompress ${DECOMPRESS_COMMAND}")
message(STATUS "  Input: ${OUTPUT_BASE}.gz")
message(STATUS "  Output: ${OUTPUT_BASE}")

if(FILEMODE)
    exec_files("${DECOMPRESS_COMMAND}" "${OUTPUT_BASE}.gz")
else()
    exec_streams("${DECOMPRESS_COMMAND}" "${OUTPUT_BASE}.gz" "${OUTPUT_BASE}")
endif()

if(CMD_RESULT)
    cleanup()
    message(FATAL_ERROR "Decompress failed: ${CMD_RESULT}")
endif()

if(COMPARE)
    message(STATUS "Diff comparison")
    message(STATUS "  Input: ${INPUT}")
    message(STATUS "  Output: ${OUTPUT_BASE}")

    # Compare decompressed output with original input file
    execute_process(COMMAND ${CMAKE_COMMAND}
        -E compare_files ${INPUT} ${OUTPUT_BASE}
        RESULT_VARIABLE CMD_RESULT)

    if(CMD_RESULT)
        diff(${INPUT} ${OUTPUT_BASE})
        cleanup()
        message(FATAL_ERROR "Compare decompress failed: ${CMD_RESULT}")
    endif()
endif()

if(GZIP_VERIFY AND NOT "${COMPRESS_ARGS}" MATCHES "-T")
    # Transparent writing does not use gzip format
    find_program(GZIP gzip)
    if(GZIP)
        if(NOT EXISTS ${OUTPUT_BASE}.gz)
            cleanup()
            message(FATAL_ERROR "Cannot find gzip decompress input: ${OUTPUT_BASE}.gz")
        endif()

        # Check gzip can decompress our compressed output
        set(GZ_DECOMPRESS_COMMAND ${GZIP} -d)

        message(STATUS "Gzip decompress ${GZ_DECOMPRESS_COMMAND}")
        message(STATUS "  Input: ${OUTPUT_BASE}.gz")
        message(STATUS "  Output: ${OUTPUT_BASE}-ungzip")

        exec_streams("${GZ_DECOMPRESS_COMMAND}" "${OUTPUT_BASE}.gz" "${OUTPUT_BASE}-ungzip")

        if(CMD_RESULT)
            cleanup()
            message(FATAL_ERROR "Gzip decompress failed: ${CMD_RESULT}")
        endif()

        # Compare gzip output with original input file
        execute_process(COMMAND ${CMAKE_COMMAND}
            -E compare_files ${INPUT} ${OUTPUT_BASE}-ungzip
            RESULT_VARIABLE CMD_RESULT)

        if(CMD_RESULT)
            diff(${INPUT} ${OUTPUT_BASE}-ungzip)
            cleanup()
            message(FATAL_ERROR "Compare gzip decompress failed: ${CMD_RESULT}")
        endif()

        # Compress input file with gzip
        set(GZ_COMPRESS_COMMAND ${GZIP} --stdout)

        message(STATUS "Gzip compress ${GZ_COMPRESS_COMMAND}")
        message(STATUS "  Input: ${INPUT}")
        message(STATUS "  Output: ${OUTPUT_BASE}-gzip.gz")

        exec_streams("${GZ_COMPRESS_COMMAND}" "${INPUT}" "${OUTPUT_BASE}-gzip.gz")

        if(CMD_RESULT)
            cleanup()
            message(FATAL_ERROR "Gzip compress failed: ${CMD_RESULT}")
        endif()

        if(NOT EXISTS ${OUTPUT_BASE}-gzip.gz)
            cleanup()
            message(FATAL_ERROR "Cannot find decompress gzip input: ${OUTPUT_BASE}-gzip.gz")
        endif()

        message(STATUS "Decompress gzip ${DECOMPRESS_COMMAND}")
        message(STATUS "  Input: ${OUTPUT_BASE}-gzip.gz")
        message(STATUS "  Output: ${OUTPUT_BASE}-gzip")

        # Check decompress target can handle gzip compressed output
        if(FILEMODE)
            exec_files("${DECOMPRESS_COMMAND}" "${OUTPUT_BASE}-gzip.gz")
        else()
            exec_streams("${DECOMPRESS_COMMAND}" "${OUTPUT_BASE}-gzip.gz" "${OUTPUT_BASE}-gzip")
        endif()

        if(CMD_RESULT)
            cleanup()
            message(FATAL_ERROR "Decompress gzip failed: ${CMD_RESULT}")
        endif()

        if(COMPARE)
            # Compare original input file with gzip decompressed output
            execute_process(COMMAND ${CMAKE_COMMAND}
                -E compare_files ${INPUT} ${OUTPUT_BASE}-gzip
                RESULT_VARIABLE CMD_RESULT)

            if(CMD_RESULT)
                diff(${INPUT} ${OUTPUT_BASE}-gzip)
                cleanup()
                message(FATAL_ERROR "Compare decompress gzip failed: ${CMD_RESULT}")
            endif()
        endif()
    endif()
endif()

cleanup_always()
