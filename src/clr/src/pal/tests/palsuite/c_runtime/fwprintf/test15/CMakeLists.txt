cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test15.c
)

add_executable(paltest_fwprintf_test15
  ${SOURCES}
)

add_dependencies(paltest_fwprintf_test15 coreclrpal)

target_link_libraries(paltest_fwprintf_test15
  pthread
  m
  coreclrpal
)
