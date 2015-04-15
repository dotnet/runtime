cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(TESTSOURCES
  WFSOProcessTest.c
)

add_executable(paltest_waitforsingleobject_wfsoprocesstest
  ${TESTSOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsoprocesstest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsoprocesstest
  pthread
  m
  coreclrpal
)


set(HELPERSOURCES
  ChildProcess.c
)

add_executable(paltest_waitforsingleobject_wfsoprocesstest_child
  ${HELPERSOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsoprocesstest_child coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsoprocesstest_child
  pthread
  m
  coreclrpal
)
