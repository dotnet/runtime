cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  writefile.c
)

add_executable(paltest_writefile_test5
  ${SOURCES}
)

add_dependencies(paltest_writefile_test5 coreclrpal)

target_link_libraries(paltest_writefile_test5
  pthread
  m
  coreclrpal
)
