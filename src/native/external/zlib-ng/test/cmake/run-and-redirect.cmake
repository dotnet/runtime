# run-and-redirect.cmake -- Runs a command and validates exit code

# Copyright (C) 2021 Nathan Moinvaziri
# Licensed under the Zlib license, see LICENSE.md for details

# Normally ctest will always fail with non-zero exit code, but we have tests
# that need to check specific exit codes.

# Required Variables
#   COMMAND      - Command to run

# Optional Variables
#   INPUT        - Standard input
#   OUTPUT       - Standard output (default: /dev/null)
#   SUCCESS_EXIT - List of successful exit codes (default: 0, ie: 0;1)

# If no output is specified, discard output
if(NOT DEFINED OUTPUT)
    if(WIN32)
        set(OUTPUT NUL)
    else()
        set(OUTPUT /dev/null)
    endif()
endif()

if(INPUT)
    # Check to see that input file exists
    if(NOT EXISTS ${INPUT})
        message(FATAL_ERROR "Cannot find input: ${INPUT}")
    endif()
    # Execute with both stdin and stdout file
    execute_process(COMMAND ${COMMAND}
        RESULT_VARIABLE CMD_RESULT
        INPUT_FILE ${INPUT}
        OUTPUT_FILE ${OUTPUT})
else()
    # Execute with only stdout file
    execute_process(COMMAND ${COMMAND}
        RESULT_VARIABLE CMD_RESULT
        OUTPUT_FILE ${OUTPUT})
endif()

# Check if exit code is in list of successful exit codes
if(SUCCESS_EXIT)
    list(FIND SUCCESS_EXIT ${CMD_RESULT} _INDEX)
    if (${_INDEX} GREATER -1)
        set(CMD_RESULT 0)
    endif()
endif()

# Check to see if successful
if(CMD_RESULT)
    message(FATAL_ERROR "${COMMAND} failed: ${CMD_RESULT}")
endif()
