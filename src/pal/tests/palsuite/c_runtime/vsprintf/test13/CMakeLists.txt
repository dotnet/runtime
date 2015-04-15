cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test13.c
)

add_executable(paltest_vsprintf_test13
  ${SOURCES}
)

add_dependencies(paltest_vsprintf_test13 coreclrpal)

target_link_libraries(paltest_vsprintf_test13
  pthread
  m
  coreclrpal
)
