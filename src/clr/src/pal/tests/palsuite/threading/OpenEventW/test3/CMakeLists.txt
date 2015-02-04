cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test3.c
)

add_executable(paltest_openeventw_test3
  ${TESTSOURCES}
)

add_dependencies(paltest_openeventw_test3 CoreClrPal)

target_link_libraries(paltest_openeventw_test3
  pthread
  m
  CoreClrPal
)


set(HELPERSOURCES
  childprocess.c
)

add_executable(paltest_openeventw_test3_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_openeventw_test3_child CoreClrPal)

target_link_libraries(paltest_openeventw_test3_child
  pthread
  m
  CoreClrPal
)
