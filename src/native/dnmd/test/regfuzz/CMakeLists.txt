set(HEADERS
    ./baseline.h
    ./fixtures.h
    ./asserts.h
)
set(SOURCES
    ./metadata.cpp
)

add_executable(regfuzz ${SOURCES} ${HEADERS})
target_link_libraries(regfuzz PRIVATE dnmd::interfaces gtest gmock minipal_com regpal) # Reference gmock for better collection assertions
if (CMAKE_SYSTEM_NAME STREQUAL "Linux")
    # Export FuzzTest's coverage hooks to the instrumented DNMD shared library.
    target_compile_options(fuzztest_coverage PRIVATE -fvisibility=default)
    target_link_options(regfuzz PRIVATE -Wl,--export-dynamic)
endif()
set_target_properties(regfuzz PROPERTIES CXX_STANDARD 17 CXX_STANDARD_REQUIRED ON) # Require C++17 for the tests so we can use std::filesystem.

target_compile_definitions(regfuzz PRIVATE COM_NO_WINDOWS_H)

if (NOT WIN32)
    target_link_libraries(regfuzz PRIVATE minipal_comhdrs)
else()
    target_link_libraries(regfuzz PRIVATE WIL)
endif()

add_custom_command(TARGET regfuzz POST_BUILD
                   COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:dnmd::interfaces> $<TARGET_FILE_DIR:regfuzz>)

if(WIN32)
    add_custom_command(TARGET regfuzz POST_BUILD
                        COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:nethost> $<TARGET_FILE_DIR:regfuzz>)
endif()

link_fuzztest(regfuzz)
