cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test6.c
)

add_executable(paltest_waitformultipleobjectsex_test6
  ${TESTSOURCES}
)

add_dependencies(paltest_waitformultipleobjectsex_test6 CoreClrPal)

target_link_libraries(paltest_waitformultipleobjectsex_test6
  pthread
  m
  CoreClrPal
)


set(HELPERSOURCES
  child6.c
)

add_executable(paltest_waitformultipleobjectsex_test6_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_waitformultipleobjectsex_test6_child CoreClrPal)

target_link_libraries(paltest_waitformultipleobjectsex_test6_child
  pthread
  m
  CoreClrPal
)
