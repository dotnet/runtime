cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  main.c
  mutex.c
)

add_executable(paltest_composite_wfmo
  ${SOURCES}
)

add_dependencies(paltest_composite_wfmo coreclrpal)

target_link_libraries(paltest_composite_wfmo
  pthread
  rt
  m
  coreclrpal
)
