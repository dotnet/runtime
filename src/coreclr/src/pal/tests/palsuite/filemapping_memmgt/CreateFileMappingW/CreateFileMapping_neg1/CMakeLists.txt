cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  CreateFileMapping_neg.c
)

add_executable(paltest_createfilemappingw_createfilemapping_neg1
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_createfilemapping_neg1 coreclrpal)

target_link_libraries(paltest_createfilemappingw_createfilemapping_neg1
  pthread
  m
  coreclrpal
)
