# test-cves.cmake -- Tests targeting common vulnerabilities and exposures

set(CVES CVE-2002-0059 CVE-2004-0797 CVE-2005-1849 CVE-2005-2096)
foreach(cve ${CVES})
    set(CVE_COMMAND ${CMAKE_CROSSCOMPILING_EMULATOR} $<TARGET_FILE:minigzip> -d)
    add_test(NAME ${cve}
        COMMAND ${CMAKE_COMMAND}
        "-DCOMMAND=${CVE_COMMAND}"
        -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/${cve}/test.gz
        "-DSUCCESS_EXIT=0;1"
        -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/run-and-redirect.cmake)
endforeach()

set(CVE_COMPRESS_LEVELS 6 1 2)
foreach(cve_compress_level ${CVE_COMPRESS_LEVELS})
    add_test(NAME CVE-2018-25032-fixed-level-${cve_compress_level}
        COMMAND ${CMAKE_COMMAND}
        "-DTARGET=${MINIDEFLATE_COMMAND}"
        "-DCOMPRESS_ARGS=-c;-k;-m;1;-w;-15;-s;4;-F;-${cve_compress_level}"
        "-DDECOMPRESS_ARGS=-c;-k;-d;-m;1;-w;-15;-${cve_compress_level}"
        -DGZIP_VERIFY=OFF
        -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/CVE-2018-25032/fixed.txt
        -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)

    add_test(NAME CVE-2018-25032-default-level-${cve_compress_level}
        COMMAND ${CMAKE_COMMAND}
        "-DTARGET=${MINIDEFLATE_COMMAND}"
        "-DCOMPRESS_ARGS=-c;-k;-m;1;-w;-15;-s;4;-${cve_compress_level}"
        "-DDECOMPRESS_ARGS=-c;-k;-d;-m;1;-w;-15;-${cve_compress_level}"
        -DGZIP_VERIFY=OFF
        -DINPUT=${CMAKE_CURRENT_SOURCE_DIR}/CVE-2018-25032/default.txt
        -P ${CMAKE_CURRENT_SOURCE_DIR}/cmake/compress-and-verify.cmake)
endforeach()
