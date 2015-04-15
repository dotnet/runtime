cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  mainWrapper.c
  threadsuspension.c
)

add_executable(paltest_threading_threadsuspension_switchthread
  ${SOURCES}
)

add_dependencies(paltest_threading_threadsuspension_switchthread coreclrpal)

target_link_libraries(paltest_threading_threadsuspension_switchthread
  pthread
  m
  coreclrpal
)
