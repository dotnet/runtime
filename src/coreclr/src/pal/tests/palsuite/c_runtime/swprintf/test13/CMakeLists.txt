cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test13.cpp
)

add_executable(paltest_swprintf_test13
  ${SOURCES}
)

add_dependencies(paltest_swprintf_test13 coreclrpal)

target_link_libraries(paltest_swprintf_test13
  ${COMMON_TEST_LIBRARIES}
)
