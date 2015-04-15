cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  processId.c
)

add_executable(paltest_getcurrentprocessid_test1
  ${SOURCES}
)

add_dependencies(paltest_getcurrentprocessid_test1 coreclrpal)

target_link_libraries(paltest_getcurrentprocessid_test1
  pthread
  m
  coreclrpal
)
