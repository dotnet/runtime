cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test4.cpp
)

add_executable(paltest_writeprocessmemory_test4
  ${TESTSOURCES}
)

add_dependencies(paltest_writeprocessmemory_test4 coreclrpal)

target_link_libraries(paltest_writeprocessmemory_test4
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  helper.cpp
)

add_executable(paltest_writeprocessmemory_test4_helper
  ${HELPERSOURCES}
)

add_dependencies(paltest_writeprocessmemory_test4_helper coreclrpal)

target_link_libraries(paltest_writeprocessmemory_test4_helper
  ${COMMON_TEST_LIBRARIES}
)
