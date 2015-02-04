cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  parentprocess.c
)

add_executable(paltest_createprocessw_test2
  ${TESTSOURCES}
)

add_dependencies(paltest_createprocessw_test2 CoreClrPal)

target_link_libraries(paltest_createprocessw_test2
  pthread
  m
  CoreClrPal
)


set(HELPERSOURCES
  childprocess.c
)

add_executable(paltest_createprocessw_test2_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_createprocessw_test2_child CoreClrPal)

target_link_libraries(paltest_createprocessw_test2_child
  pthread
  m
  CoreClrPal
)
