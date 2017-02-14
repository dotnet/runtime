cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  getc.cpp
)

add_executable(paltest_getc_test1
  ${SOURCES}
)

add_dependencies(paltest_getc_test1 coreclrpal)

target_link_libraries(paltest_getc_test1
  ${COMMON_TEST_LIBRARIES}
)
