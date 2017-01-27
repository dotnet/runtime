cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test6.cpp
)

add_executable(paltest_lockfile_test6
  ${TESTSOURCES}
)

add_dependencies(paltest_lockfile_test6 coreclrpal)

target_link_libraries(paltest_lockfile_test6
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  helper.cpp
)

add_executable(paltest_lockfile_test6_helper
  ${HELPERSOURCES}
)

add_dependencies(paltest_lockfile_test6_helper coreclrpal)

target_link_libraries(paltest_lockfile_test6_helper
  ${COMMON_TEST_LIBRARIES}
)
