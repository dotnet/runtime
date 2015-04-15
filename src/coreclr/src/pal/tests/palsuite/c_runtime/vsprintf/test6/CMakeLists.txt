cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test6.c
)

add_executable(paltest_vsprintf_test6
  ${SOURCES}
)

add_dependencies(paltest_vsprintf_test6 coreclrpal)

target_link_libraries(paltest_vsprintf_test6
  pthread
  m
  coreclrpal
)
