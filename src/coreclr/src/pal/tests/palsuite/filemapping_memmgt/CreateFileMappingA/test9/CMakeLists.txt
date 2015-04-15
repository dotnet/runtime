cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  createfilemapping.c
)

add_executable(paltest_createfilemappinga_test9
  ${SOURCES}
)

add_dependencies(paltest_createfilemappinga_test9 coreclrpal)

target_link_libraries(paltest_createfilemappinga_test9
  pthread
  m
  coreclrpal
)
