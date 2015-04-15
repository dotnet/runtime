cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  WFSOThreadTest.c
)

add_executable(paltest_waitforsingleobject_wfsothreadtest
  ${SOURCES}
)

add_dependencies(paltest_waitforsingleobject_wfsothreadtest coreclrpal)

target_link_libraries(paltest_waitforsingleobject_wfsothreadtest
  pthread
  m
  coreclrpal
)
