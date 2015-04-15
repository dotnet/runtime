cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  main.c
  mutex.c
)

add_executable(paltest_mutex_shared
  ${SOURCES}
)

add_dependencies(paltest_mutex_shared coreclrpal)

target_link_libraries(paltest_mutex_shared
  pthread
  rt
  m
  coreclrpal
)
