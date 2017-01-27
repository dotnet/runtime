cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  writefile.cpp
)

add_executable(paltest_writefile_test4
  ${SOURCES}
)

add_dependencies(paltest_writefile_test4 coreclrpal)

target_link_libraries(paltest_writefile_test4
  ${COMMON_TEST_LIBRARIES}
)
