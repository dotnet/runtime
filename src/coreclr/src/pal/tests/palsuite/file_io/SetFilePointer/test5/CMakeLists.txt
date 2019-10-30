cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  SetFilePointer.cpp
)

add_executable(paltest_setfilepointer_test5
  ${SOURCES}
)

add_dependencies(paltest_setfilepointer_test5 coreclrpal)

target_link_libraries(paltest_setfilepointer_test5
  ${COMMON_TEST_LIBRARIES}
)
