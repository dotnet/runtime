cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  criticalsection.c
  mainWrapper.c
)

add_executable(paltest_synchronization_criticalsection
  ${SOURCES}
)

add_dependencies(paltest_synchronization_criticalsection coreclrpal)

target_link_libraries(paltest_synchronization_criticalsection
  pthread
  m
  coreclrpal
)
