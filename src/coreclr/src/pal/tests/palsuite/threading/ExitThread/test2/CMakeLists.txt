cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  test2.c
)

add_executable(paltest_exitthread_test2
  ${TESTSOURCES}
)

add_dependencies(paltest_exitthread_test2 coreclrpal)

target_link_libraries(paltest_exitthread_test2
  pthread
  m
  coreclrpal
)


set(HELPERSOURCES
  childprocess.c
)

add_executable(paltest_exitthread_test2_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_exitthread_test2_child coreclrpal)

target_link_libraries(paltest_exitthread_test2_child
  pthread
  m
  coreclrpal
)
