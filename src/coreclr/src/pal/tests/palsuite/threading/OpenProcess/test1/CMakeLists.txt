cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test1.c
)

add_executable(paltest_openprocess_test1
  ${TESTSOURCES}
)

add_dependencies(paltest_openprocess_test1 coreclrpal)

target_link_libraries(paltest_openprocess_test1
  pthread
  m
  coreclrpal
)


set(HELPERSOURCES
  childProcess.c
)

add_executable(paltest_openprocess_test1_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_openprocess_test1_child coreclrpal)

target_link_libraries(paltest_openprocess_test1_child
  pthread
  m
  coreclrpal
)
