cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  DeleteFileW.cpp
)

add_executable(paltest_deletefilew_test1
  ${SOURCES}
)

add_dependencies(paltest_deletefilew_test1 coreclrpal)

target_link_libraries(paltest_deletefilew_test1
  ${COMMON_TEST_LIBRARIES}
)
