cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  WFSOExMutexTest.c
)

add_executable(paltest_waitforsingleobject_wfsoexmutextest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsoexmutextest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsoexmutextest
  pthread
  m
  coreclrpal
)
