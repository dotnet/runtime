cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  createfilemapping.c
)

add_executable(paltest_createfilemappingw_test8
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test8 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test8
  pthread
  m
  coreclrpal
)
