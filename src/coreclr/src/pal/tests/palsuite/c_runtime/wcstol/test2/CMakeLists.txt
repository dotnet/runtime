cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test2.cpp
)

add_executable(paltest_wcstol_test2
  ${SOURCES}
)

add_dependencies(paltest_wcstol_test2 coreclrpal)

target_link_libraries(paltest_wcstol_test2
  ${COMMON_TEST_LIBRARIES}
)
