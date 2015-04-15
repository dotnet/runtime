cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test11.c
)

add_executable(paltest_fprintf_test11
  ${SOURCES}
)

add_dependencies(paltest_fprintf_test11 coreclrpal)

target_link_libraries(paltest_fprintf_test11
  pthread
  m
  coreclrpal
)
