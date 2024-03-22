# test-issues.cmake -- Tests targeting specific GitHub issues

add_test(NAME GH-361
    COMMAND ${CMAKE_COMMAND}
    "-DTARGET=${MINIGZIP_COMMAND}"
    "-DCOMPRESS_ARGS=-c;-k;-4"
    -DTEST_NAME=GH-361-test-txt
    -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/GH-361/test.txt
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)

add_test(NAME GH-364
    COMMAND ${CMAKE_COMMAND}
    "-DCOMPRESS_TARGET=${SWITCHLEVELS_COMMAND}"
    "-DCOMPRESS_ARGS=1;5;9;3"
    "-DDECOMPRESS_TARGET=${MINIGZIP_COMMAND}"
    -DTEST_NAME=GH-364-test-bin
    -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/GH-364/test.bin
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)

add_test(NAME GH-382
    COMMAND ${CMAKE_COMMAND}
    "-DTARGET=${MINIDEFLATE_COMMAND}"
    "-DCOMPRESS_ARGS=-c;-m;1;-w;-15;-1;-s;4"
    "-DDECOMPRESS_ARGS=-c;-d;-m;1;-w;-15"
    -DGZIP_VERIFY=OFF
    -DTEST_NAME=GH-382-defneg3-dat
    -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/GH-382/defneg3.dat
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)

add_test(NAME GH-536-segfault
    COMMAND ${CMAKE_COMMAND}
    "-DCOMPRESS_TARGET=${SWITCHLEVELS_COMMAND}"
    "-DCOMPRESS_ARGS=6;9744;1;91207"
    "-DDECOMPRESS_TARGET=${MINIGZIP_COMMAND}"
    -DCOMPARE=OFF
    -DGZIP_VERIFY=OFF
    -DTEST_NAME=GH-536-segfault-lcet10-txt
    -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/data/lcet10.txt
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)

add_test(NAME GH-536-incomplete-read
    COMMAND ${CMAKE_COMMAND}
    "-DCOMPRESS_TARGET=${SWITCHLEVELS_COMMAND}"
    "-DCOMPRESS_ARGS=6;88933;1;195840;2;45761"
    "-DDECOMPRESS_TARGET=${MINIGZIP_COMMAND}"
    -DCOMPARE=OFF
    -DGZIP_VERIFY=OFF
    -DTEST_NAME=GH-536-incomplete-read-lcet10-txt
    -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/data/lcet10.txt
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)

add_test(NAME GH-536-zero-stored-block
    COMMAND ${CMAKE_COMMAND}
    "-DCOMPRESS_TARGET=${SWITCHLEVELS_COMMAND}"
    "-DCOMPRESS_ARGS=6;15248;1;1050;2;25217"
    "-DDECOMPRESS_TARGET=${MINIGZIP_COMMAND}"
    -DCOMPARE=OFF
    -DGZIP_VERIFY=OFF
    -DTEST_NAME=GH-536-zero-stored-block-lcet10-txt
    -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/data/lcet10.txt
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)

add_test(NAME GH-751
    COMMAND ${CMAKE_COMMAND}
    "-DTARGET=${MINIGZIP_COMMAND}"
    -DTEST_NAME=GH-751-test-txt
    -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/GH-751/test.txt
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)

set(TEST_COMMAND "${MINIDEFLATE_COMMAND};-c;-d;-k;-s;4")
add_test(NAME GH-1600-no-window-check
    COMMAND ${CMAKE_COMMAND}
    "-DCOMMAND=${TEST_COMMAND}"
    "-DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/GH-1600/packobj.gz"
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/run-and-redirect.cmake
)

set(TEST_COMMAND "${MINIDEFLATE_COMMAND};-c;-d;-k;-s;4;-r;25")
add_test(NAME GH-1600-no-window-no-check
    COMMAND ${CMAKE_COMMAND}
    "-DCOMMAND=${TEST_COMMAND}"
    "-DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/GH-1600/packobj.gz"
    -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/run-and-redirect.cmake
)
