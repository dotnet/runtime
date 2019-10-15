cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  mainWrapper.cpp
  threadsuspension.cpp
)

add_executable(paltest_threading_threadsuspension
  ${SOURCES}
)

add_dependencies(paltest_threading_threadsuspension coreclrpal)

target_link_libraries(paltest_threading_threadsuspension
  ${COMMON_TEST_LIBRARIES}
)
