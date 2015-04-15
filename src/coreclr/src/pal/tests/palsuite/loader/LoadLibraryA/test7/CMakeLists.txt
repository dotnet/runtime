cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  LoadLibraryA.c
)

add_executable(paltest_loadlibrarya_test7
  ${SOURCES}
)

add_dependencies(paltest_loadlibrarya_test7 coreclrpal)

target_link_libraries(paltest_loadlibrarya_test7
  pthread
  m
  coreclrpal
)
