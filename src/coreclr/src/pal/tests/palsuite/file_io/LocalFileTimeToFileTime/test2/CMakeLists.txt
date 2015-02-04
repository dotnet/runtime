cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  localfiletimetofiletime.c
)

add_executable(paltest_localfiletimetofiletime_test2
  ${SOURCES}
)

add_dependencies(paltest_localfiletimetofiletime_test2 CoreClrPal)

target_link_libraries(paltest_localfiletimetofiletime_test2
  pthread
  m
  CoreClrPal
)
