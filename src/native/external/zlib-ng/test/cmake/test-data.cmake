# test-data.cmake - Tests targeting data files in the data directory

# Test compress and verify test against data file using extra args
macro(test_minigzip name path)
    # Construct compression arguments for minigzip
    set(compress_args -k -c)
    foreach(extra_arg IN ITEMS "${ARGN}")
        list(APPEND compress_args ${extra_arg})
    endforeach()

    # Create unique friendly string for test
    string(REPLACE ";" "" arg_list "${ARGN}")
    string(REPLACE " " "" arg_list "${arg_list}")
    string(REPLACE "-" "" arg_list "${arg_list}")

    set(test_id minigzip-${name}-${arg_list})

    if(NOT TEST ${test_id})
        add_test(NAME ${test_id}
            COMMAND ${CMAKE_COMMAND}
            "-DTARGET=${MINIGZIP_COMMAND}"
            "-DCOMPRESS_ARGS=${compress_args}"
            "-DDECOMPRESS_ARGS=-d;-c"
            -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/${path}
            -DTEST_NAME=${test_id}
            -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)
    endif()
endmacro()

# List of arg combinations to use during compression
set(TEST_CONFIGS
    -R      # Z_RLE
    -h      # Z_HUFFMAN_ONLY
    -T      # Direct store
    -0      # No compression
    -1      # Deflate quick
    -2      # Deflate fast
    -4      # Deflate medium (lazy matches)
    "-5;-F" # Deflate medium (Z_FIXED)
    -6      # Deflate medium
    -9      # Deflate slow
    "-9;-f" # Deflate slow (Z_FILTERED)
)

# Enumerate all files in data directory to run tests against
file(GLOB_RECURSE TEST_FILE_PATHS
    LIST_DIRECTORIES false
    RELATIVE ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_SOURCE_DIR}/data/*)

# For all files in the data directory, run tests against them
foreach(test_file_path ${TEST_FILE_PATHS})
    if("${test_file_path}" MATCHES ".gz$" OR "${test_file_path}" MATCHES ".out$" OR
        "${test_file_path}" MATCHES "/.git/" OR "${test_file_path}" MATCHES ".md$")
        continue()
    endif()
    foreach(test_config ${TEST_CONFIGS})
        get_filename_component(test_name ${test_file_path} NAME)
        if (test_name STREQUAL "")
            continue()
        endif()
        test_minigzip(${test_name} ${test_file_path} ${test_config})
    endforeach()
endforeach()

# Additional tests to verify with automatic data type detection arg
test_minigzip("detect-text" "data/lcet10.txt" -A)
test_minigzip("detect-binary" "data/paper-100k.pdf" -A)
