cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  test1.cpp
)

add_executable(paltest_open_osfhandle_test1
  ${SOURCES}
)

add_dependencies(paltest_open_osfhandle_test1 coreclrpal)

target_link_libraries(paltest_open_osfhandle_test1
  ${COMMON_TEST_LIBRARIES}
)
