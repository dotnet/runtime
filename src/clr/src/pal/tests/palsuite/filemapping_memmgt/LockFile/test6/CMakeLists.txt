cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test6.c
)

add_executable(paltest_lockfile_test6
  ${TESTSOURCES}
)

add_dependencies(paltest_lockfile_test6 CoreClrPal)

target_link_libraries(paltest_lockfile_test6
  pthread
  m
  CoreClrPal
)


set(HELPERSOURCES
  helper.c
)

add_executable(paltest_lockfile_test6_helper
  ${HELPERSOURCES}
)

add_dependencies(paltest_lockfile_test6_helper CoreClrPal)

target_link_libraries(paltest_lockfile_test6_helper
  pthread
  m
  CoreClrPal
)
