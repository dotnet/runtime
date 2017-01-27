cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test19.cpp
)

add_executable(paltest_sprintf_test19
  ${SOURCES}
)

add_dependencies(paltest_sprintf_test19 coreclrpal)

target_link_libraries(paltest_sprintf_test19
  ${COMMON_TEST_LIBRARIES}
)
