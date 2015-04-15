cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  WFSOMutexTest.c
)

add_executable(paltest_waitforsingleobject_wfsomutextest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsomutextest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsomutextest
  pthread
  m
  coreclrpal
)
